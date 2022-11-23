using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

using Elanetic.Graphics;

public class VRRFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        RenderTexture FuckTex;
        RenderTexture FuckOutput;
        RawImage rawImageFuck;

        enum EventID
        {
            event_BeginVRRPass = 0,
            event_EndVRRPass = 1,
            event_DoVRRPostPass = 2,
            event_DrawSimpleTriangle = 3,
            event_DrawMixSimpleTriangle = 4,
            event_DrawMesh = 5,
        };

        [StructLayout(LayoutKind.Sequential)]
        struct TriangleData
        {
            public IntPtr colorTex;
            public float w;
            public float h;
            public float t;
            public int validatedata;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct VRRPassData
        {
            public IntPtr colorTex;
            public IntPtr outputTex;
            public int validatedata;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DrawData
        {
            public IntPtr vertexBuffer;
            public IntPtr indexBuffer;
            public IntPtr textureBuffer;
            public IntPtr localToWorld;
        }

        private GameObject m_BuildinItem;
        private MeshFilter m_BuildinMF;
        private Renderer m_BuildinRR;

        public CustomRenderPass()
        {

            //Load textures
            FuckTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            FuckTex.name = "TmpColorTex";
            FuckTex.Create();

            FuckOutput = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            FuckOutput.name = "TmpColorOutputTex";
            FuckOutput.Create();

            rawImageFuck = GameObject.Find("RawImageFuck").GetComponent<RawImage>();
            if (rawImageFuck)
            {
                rawImageFuck.texture = FuckOutput;
                rawImageFuck.color = Color.white;
            }
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        GCHandle _beginPassArgs;
        GCHandle _blitPassArgs;
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BuildinItem)
            {
                m_BuildinMF = m_BuildinItem.GetComponentInChildren<MeshFilter>();
                m_BuildinRR = m_BuildinItem.GetComponentInChildren<Renderer>();

                m_BuildinRR.enabled = false;
            }
            else
            {
                m_BuildinItem = GameObject.Find("Pikachu");
            }

            if(m_BuildinItem)
            {
                CommandBuffer pikaCmd = CommandBufferPool.Get("PikaCM");
                for(int i = 0; i < m_BuildinRR.sharedMaterials.Length; i++)
                {
                    Material subMat = m_BuildinRR.sharedMaterials[i];
                    pikaCmd.DrawMesh((m_BuildinRR is SkinnedMeshRenderer) ? ((SkinnedMeshRenderer)m_BuildinRR).sharedMesh : m_BuildinMF.sharedMesh, m_BuildinRR.localToWorldMatrix, subMat, i, 0);
                }
                context.ExecuteCommandBuffer(pikaCmd);
                pikaCmd.Clear();
                CommandBufferPool.Release(pikaCmd);
            }
            

            #if (!UNITY_EDITOR && UNITY_IOS)
            CommandBuffer cmd = CommandBufferPool.Get("FuckCMD");

            unsafe
            {
                _beginPassArgs = GCHandle.Alloc(
                    new TriangleData
                    {
                        colorTex = FuckTex.colorBuffer.GetNativeRenderBufferPtr(),
                        w = FuckTex.width,
                        h = FuckTex.height,
                        t = Time.timeSinceLevelLoad,
                        validatedata = 123,
                    },
                    GCHandleType.Pinned
                );

                _blitPassArgs = GCHandle.Alloc(
                   new VRRPassData
                   {
                       colorTex = FuckTex.colorBuffer.GetNativeRenderBufferPtr(),
                       outputTex = FuckOutput.colorBuffer.GetNativeRenderBufferPtr(),
                       validatedata = 456,
                   },
                   GCHandleType.Pinned
               );
            }

            Debug.Log("Before Call Event");

            if (m_BuildinItem)
            {
                Debug.Log("Draw Mix");
                DirectGraphics.BeginVRRPassCMD(cmd, (int)EventID.event_BeginVRRPass, _beginPassArgs.AddrOfPinnedObject());
                DirectGraphics.DrawMixSimpleTriangleCMD(cmd, (int)EventID.event_DrawMixSimpleTriangle);


                IntPtr indexBufferPtr = m_BuildinMF.sharedMesh.GetNativeIndexBufferPtr();
                for (int i = 0; i < m_BuildinRR.sharedMaterials.Length; i++)
                {
                    IntPtr vertexBufferPtr = m_BuildinMF.sharedMesh.GetNativeVertexBufferPtr(i);
                    Material subMat = m_BuildinRR.sharedMaterials[i];
                    IntPtr subTexBufferPtr = subMat.mainTexture.GetNativeTexturePtr();
                    DrawUnLitMesh(cmd, vertexBufferPtr, indexBufferPtr, subTexBufferPtr, m_BuildinRR.localToWorldMatrix);
                }

                //cmd.DrawMesh(m_BuildinMF.sharedMesh, m_BuildinItem.transform.localToWorldMatrix, m_BuildinRR.sharedMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                DirectGraphics.EndVRRPassCMD(cmd, (int)EventID.event_EndVRRPass);
            }
            else
            {
                Debug.Log("Draw Normal");
                DirectGraphics.DrawSimpleTriangleCMD(cmd, (int)EventID.event_DrawSimpleTriangle, _beginPassArgs.AddrOfPinnedObject());
            }

            DirectGraphics.DrawVRRBlitCMD(cmd, (int)EventID.event_DoVRRPostPass, _blitPassArgs.AddrOfPinnedObject());
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
            #endif
        }

        private float[] m_tmpLocalToWorldMatrixBuffer = new float[16];
        private GCHandle drawHandle;
        void DrawUnLitMesh(CommandBuffer cmd, IntPtr pVertexBuffer, IntPtr pIndexBuffer, IntPtr pTextureBuffer, Matrix4x4 pLocalToWorldMatrix)
        {
            for(int i = 0; i < 16; i++)
            {
                m_tmpLocalToWorldMatrixBuffer[i] = pLocalToWorldMatrix[i];
            }

            unsafe
            {
                drawHandle = GCHandle.Alloc(
                    new DrawData {
                        vertexBuffer = pVertexBuffer,
                        indexBuffer = pIndexBuffer,
                        textureBuffer = pTextureBuffer,
                        localToWorld = GCHandle.Alloc(m_tmpLocalToWorldMatrixBuffer).AddrOfPinnedObject()
                    }, GCHandleType.Pinned);
            }

            DirectGraphics.DrawMesh(cmd, (int)EventID.event_DrawMesh, drawHandle.AddrOfPinnedObject());
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRendering;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
//#if (!UNITY_EDITOR && UNITY_IOS)
        renderer.EnqueuePass(m_ScriptablePass);
//#endif
    }
}


