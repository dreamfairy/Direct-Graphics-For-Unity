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
        RenderTexture FuckDepthTex;
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
            public IntPtr depthTex;
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
            public IntPtr uvBuffer;
            public IntPtr textureBuffer;
            public IntPtr localToWorld;
            public int indexOffset;
            public int indexCount;
            public float time;
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

            FuckDepthTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Depth);
            FuckDepthTex.name = "TmpDepthTex";
            FuckDepthTex.Create();

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

            #if UNITY_EDITOR
            if(m_BuildinItem)
            {
                CommandBuffer pikaCmd = CommandBufferPool.Get("PikaCM");
                Mesh targetMesh = (m_BuildinRR is SkinnedMeshRenderer) ? ((SkinnedMeshRenderer)(m_BuildinRR)).sharedMesh : m_BuildinMF.sharedMesh;

                VertexAttributeDescriptor[] vertexDesc = targetMesh.GetVertexAttributes();

                for(int i = 0; i < m_BuildinRR.sharedMaterials.Length; i++)
                {
                    int subMeshIndexOffset = (int)targetMesh.GetIndexStart(i);
                    int subMeshIndexCount = (int)targetMesh.GetIndexCount(i);

                    //Debug.Log(string.Format("SubMesh {0} start {1} offset {2}", i, subMeshIndexOffset, subMeshIndexCount));

                    Material subMat = m_BuildinRR.sharedMaterials[i];
                    Matrix4x4 trs = Matrix4x4.TRS(new Vector3(0,-0.5f+Mathf.Sin(Time.realtimeSinceStartup),1.5f), Quaternion.Euler(-90,Mathf.Sin(Time.realtimeSinceStartup) * 360,0), Vector3.one);
                    pikaCmd.DrawMesh((m_BuildinRR is SkinnedMeshRenderer) ? ((SkinnedMeshRenderer)m_BuildinRR).sharedMesh : m_BuildinMF.sharedMesh, trs, subMat, i, 0);
                }
                context.ExecuteCommandBuffer(pikaCmd);
                pikaCmd.Clear();
                CommandBufferPool.Release(pikaCmd);
            }
            #endif

            #if (!UNITY_EDITOR && UNITY_IOS)
            CommandBuffer cmd = CommandBufferPool.Get("FuckCMD");

            bool drawMesh = false;
            unsafe
            {
                _beginPassArgs = GCHandle.Alloc(
                    new TriangleData
                    {
                        colorTex = FuckTex.colorBuffer.GetNativeRenderBufferPtr(),
                        depthTex =  drawMesh ? FuckDepthTex.depthBuffer.GetNativeRenderBufferPtr() : IntPtr.Zero,
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

            if (false)
            {
                Debug.Log("Draw Mix");
                DirectGraphics.BeginVRRPassCMD(cmd, (int)EventID.event_BeginVRRPass, _beginPassArgs.AddrOfPinnedObject());
                //DirectGraphics.DrawMixSimpleTriangleCMD(cmd, (int)EventID.event_DrawMixSimpleTriangle);

                Mesh targetMesh = (m_BuildinRR is SkinnedMeshRenderer) ? ((SkinnedMeshRenderer)(m_BuildinRR)).sharedMesh : m_BuildinMF.sharedMesh;
                IntPtr indexBufferPtr = targetMesh.GetNativeIndexBufferPtr();
                IntPtr vertexBufferPtr = targetMesh.GetNativeVertexBufferPtr(0);
                IntPtr uvBufferPtr = targetMesh.GetNativeVertexBufferPtr(1);

                for (int i = 0; i < m_BuildinRR.sharedMaterials.Length; i++)
                {
                    Material subMat = m_BuildinRR.sharedMaterials[i];
                    IntPtr subTexBufferPtr = subMat.mainTexture.GetNativeTexturePtr();

                    int subMeshIndexOffset = (int)targetMesh.GetIndexStart(i);
                    int subMeshIndexCount = (int)targetMesh.GetIndexCount(i);

                    Matrix4x4 trs = Matrix4x4.TRS(new Vector3(0,-0.5f+Mathf.Sin(Time.realtimeSinceStartup),1.5f), Quaternion.Euler(-90,Mathf.Sin(Time.realtimeSinceStartup) * 360,0), Vector3.one);
                    Matrix4x4 view = renderingData.cameraData.camera.worldToCameraMatrix;
                    Matrix4x4 proj = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, true);
                    Matrix4x4 mvp = proj * view * trs;
                    
                    DrawUnLitMesh(cmd, vertexBufferPtr, indexBufferPtr, uvBufferPtr, subTexBufferPtr, subMeshIndexOffset, subMeshIndexCount, mvp);
                }
                DirectGraphics.EndVRRPassCMD(cmd, (int)EventID.event_EndVRRPass);
                //cmd.DrawMesh(m_BuildinMF.sharedMesh, m_BuildinItem.transform.localToWorldMatrix, m_BuildinRR.sharedMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
               
            }
            else
            {
                Debug.Log("Draw Normal");
                DirectGraphics.BeginVRRPassCMD(cmd, (int)EventID.event_BeginVRRPass, _beginPassArgs.AddrOfPinnedObject());
                //DirectGraphics.DrawSimpleTriangleCMD(cmd, (int)EventID.event_DrawSimpleTriangle, _beginPassArgs.AddrOfPinnedObject());
            }

           
            DirectGraphics.DrawVRRBlitCMD(cmd, (int)EventID.event_DoVRRPostPass, _blitPassArgs.AddrOfPinnedObject());
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
            #endif
        }

        private float[] m_tmpLocalToWorldMatrixBuffer = new float[16];
        private GCHandle drawHandle;
        void DrawUnLitMesh(CommandBuffer cmd, IntPtr pVertexBuffer, IntPtr pIndexBuffer, IntPtr pUVBuffer, IntPtr pTextureBuffer, int pIndexOffset, int pIndexCount, Matrix4x4 pLocalToWorldMatrix)
        {
            //Matrix4x4 transposeMat = pLocalToWorldMatrix.transpose;
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
                        uvBuffer = pUVBuffer,
                        textureBuffer = pTextureBuffer,
                        localToWorld = GCHandle.Alloc(m_tmpLocalToWorldMatrixBuffer,GCHandleType.Pinned).AddrOfPinnedObject(),
                        indexOffset = pIndexOffset,
                        indexCount = pIndexCount,
                        time = Time.timeSinceLevelLoad,
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


