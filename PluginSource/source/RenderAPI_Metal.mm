
#include "RenderAPI.h"
#include "PlatformBase.h"


// Metal implementation of RenderAPI.


#if SUPPORT_METAL

#include "Unity/IUnityGraphicsMetal.h"
#import <Metal/Metal.h>
#include <vector>


class RenderAPI_Metal : public RenderAPI
{
public:
    RenderAPI_Metal();
    virtual ~RenderAPI_Metal() { }

    virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces);

    virtual bool GetUsesReverseZ() { return true; }

    virtual void* BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch);
    virtual void EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr);

    virtual void DoCopyTexture(void *sourceTexture, int sourceX, int sourceY, int sourceWidth, int sourceHeight, void *destinationTexture, int destinationX, int destinationY);
    virtual bool CreateTexture(int width, int height, int format, int textureIndex);
    virtual void DestroyTexture(int textureIndex);
    virtual void* GetTexturePointer(int textureIndex);
    virtual void SetTextureColor(float red, float green, float blue, float alpha, void* targetTexture, float w, float h, float t);

private:
    void CreateRatemapResource();
    void CreateResources();
    void DrawSimpleTriangles( id<MTLRenderCommandEncoder> cmd, const float worldMatrix[16], int triangleCount, const void* verticesFloat3Byte4);
    void DrawColoredTriangle(id<MTLRenderCommandEncoder> cmd, float t);
    void DrawVRRBlit(void* sourceTexture, void* targetTexture);

private:
    IUnityGraphicsMetal*    m_MetalGraphics;
    id<MTLBuffer>            m_VertexBuffer;
    id<MTLBuffer>            m_ConstantBuffer;
    
    
    //vrr
    id<MTLRasterizationRateMap> g_RateMap;
    id<MTLFunction> g_VProg, g_FProg;
    id<MTLBuffer> g_VB;
    MTLVertexDescriptor* g_VertexDesc;
    MTLRenderPassDescriptor* g_RatemapPassDesc;
    MTLRenderPipelineDescriptor* g_RatemapPipelineDesc;
    id<MTLRenderPipelineState> g_VRRBlitPipelineState;
    id<MTLBuffer>           m_RatemapSizeBuffer;

    //simpletriangle
    id<MTLDepthStencilState> m_DepthStencil;
    id<MTLRenderPipelineState>    m_Pipeline;
    
    std::vector<void*> m_Textures;
    int m_UsedTextureCount;
    
    id<MTLCommandQueue> m_CommandQueue;

};


RenderAPI* CreateRenderAPI_Metal()
{
    return new RenderAPI_Metal();
}


static Class MTLVertexDescriptorClass;
static Class MTLRenderPipelineDescriptorClass;
static Class MTLDepthStencilDescriptorClass;
const int kVertexSize = 12 + 4;

// Simple vertex & fragment shader source
static const char kShaderSource[] =
"#include <metal_stdlib>\n"
"using namespace metal;\n"
"struct AppData\n"
"{\n"
"    float4x4 worldMatrix;\n"
"};\n"
"struct Vertex\n"
"{\n"
"    float3 pos [[attribute(0)]];\n"
"    float4 color [[attribute(1)]];\n"
"};\n"
"struct VSOutput\n"
"{\n"
"    float4 pos [[position]];\n"
"    half4  color;\n"
"};\n"
"struct FSOutput\n"
"{\n"
"    half4 frag_data [[color(0)]];\n"
"};\n"
"vertex VSOutput vertexMain(Vertex input [[stage_in]], constant AppData& my_cb [[buffer(0)]])\n"
"{\n"
"    VSOutput out = { my_cb.worldMatrix * float4(input.pos.xyz * 1.5, 1), (half4)input.color };\n"
"    return out;\n"
"}\n"
"fragment FSOutput fragmentMain(VSOutput input [[stage_in]])\n"
"{\n"
"    FSOutput out = { input.color };\n"
"    return out;\n"
"}\n";

static const char GMetalVRRVertexShader[] = R"""(
using namespace metal;

struct FVSFuckStageIn
{
    float4 InPosition [[ attribute(0) ]];
    float4 InTexCoord [[ attribute(1) ]];
};

struct FVSOut
{
    float2 Texcoord;
    float4 Position [[ position ]];
};

vertex FVSOut Main_VS(FVSFuckStageIn __VSStageIn [[ stage_in ]])
{
    FVSOut t2;
    t2.Texcoord.xy = float2(__VSStageIn.InTexCoord.x, 1- __VSStageIn.InTexCoord.y);
    t2.Position.xyzw = float4(__VSStageIn.InPosition.xyz, 1.0);
    return t2;
}
)""";

static NSString* GMetalVRRFragmentShader = @"#include <metal_stdlib>\n"
"using namespace metal;\n"

"struct FVSOut\n"
"{\n"
    "float2 Texcoord;\n"
    "float4 Position [[ position ]];\n"
"};\n"
"struct FPSOut\n"
"{\n"
"       float4 FragColor0 [[ color(0) ]];\n"
"};\n"
"fragment FPSOut Main_PS(FVSOut __PSStageIn [[ stage_in ]],\n"
"       texture2d<float> PostprocessInput0 [[ texture(0) ]],\n"
"    constant rasterization_rate_map_data& g_RRMData [[buffer(0)]])\n"
"{\n"
"       FPSOut t0;\n"
"    constexpr sampler readSampler(mag_filter::nearest, min_filter::nearest, address::clamp_to_zero, coord::pixel);\n"
"    rasterization_rate_map_decoder Decoder(g_RRMData);\n"
"    float2 uv        = __PSStageIn.Position.xy;\n"
"    int needDrawLine = ((__PSStageIn.Texcoord.x > 0.29 && __PSStageIn.Texcoord.x < 0.31) || (__PSStageIn.Texcoord.x > 0.69 && __PSStageIn.Texcoord.x < 0.71) || (__PSStageIn.Texcoord.y > 0.29 && __PSStageIn.Texcoord.y < 0.31) || (__PSStageIn.Texcoord.y > 0.69 && __PSStageIn.Texcoord.y < 0.71) ) ? 1 : 0;\n"
"    float2 physCoords = Decoder.map_screen_to_physical_coordinates(uv);\n"
"    float4 col = float4(PostprocessInput0.sample(readSampler, physCoords));\n"

"    t0.FragColor0.xyzw = mix(col , float4(1,0,0,1), needDrawLine);\n"
//"       t0.FragColor0.xyzw = float4(__PSStageIn.Texcoord.xy, 0, 1);\n"
"    return t0;\n"
"}\n";

void RenderAPI_Metal::CreateRatemapResource()
{
       id<MTLDevice> device = m_MetalGraphics->MetalDevice();
       NSBundle* mtlBundle = m_MetalGraphics->MetalBundle();

       MTLCompileOptions* CompileOptions;
       NSError* error = nil;

    // Create shaders
    NSString* srcStr = [[NSString alloc] initWithBytes:kShaderSource length:sizeof(kShaderSource) encoding:NSASCIIStringEncoding];

       NSString* nsVSSource = [NSString stringWithUTF8String:GMetalVRRVertexShader];
       id<MTLLibrary> VRRVertexShaderLib = [device newLibraryWithSource:nsVSSource options:CompileOptions error:&error];
    
    if(error != nil)
    {
        NSString* desc        = [error localizedDescription];
        NSString* reason    = [error localizedFailureReason];
        ::fprintf(stderr, "%s\n%s\n\n", desc ? [desc UTF8String] : "<unknown>", reason ? [reason UTF8String] : "");
    }
       id<MTLFunction> VRRVertexFunc = [VRRVertexShaderLib newFunctionWithName:@"Main_VS"];

       id<MTLLibrary> VRRFragmentShaderLib = [device newLibraryWithSource:GMetalVRRFragmentShader options:CompileOptions error:&error];
    
    if(error != nil)
    {
        NSString* desc        = [error localizedDescription];
        NSString* reason    = [error localizedFailureReason];
        ::fprintf(stderr, "%s\n%s\n\n", desc ? [desc UTF8String] : "<unknown>", reason ? [reason UTF8String] : "");
    }
    
       id<MTLFunction> VRRFragmentFunc = [VRRFragmentShaderLib newFunctionWithName:@"Main_PS"];

       const float VertexData[] =
           {
               //x,y,z,w,u,v,0,0
               -1.0f, -1.0f, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0f,//0
               -1, 1.0f, 0.0, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f,//1
               1.0f, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f,//2
               
               -1.0f, -1.0f, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0f,//0
               1.0f, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f,//2
               1.0f, -1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f,//3
           };

       float dataSize = sizeof(VertexData);

       id<MTLBuffer> VertexBuffer = [device newBufferWithBytes:VertexData length:dataSize options:MTLResourceOptionCPUCacheModeDefault];

       g_VProg = VRRVertexFunc;
       g_FProg = VRRFragmentFunc;
       g_VB = VertexBuffer;

       //pos
       MTLVertexAttributeDescriptor* PosAttrDesc = [[mtlBundle classNamed:@"MTLVertexAttributeDescriptor"] new];
       PosAttrDesc.format = MTLVertexFormatFloat4;
       PosAttrDesc.offset = 0;
       PosAttrDesc.bufferIndex = 4;

       //uv
       MTLVertexAttributeDescriptor* UVAttrDesc = [[mtlBundle classNamed:@"MTLVertexAttributeDescriptor"] new];
       UVAttrDesc.format = MTLVertexFormatFloat4;
       UVAttrDesc.offset = 16;
       UVAttrDesc.bufferIndex = 4;
       
       //layout
       MTLVertexBufferLayoutDescriptor* streamDesc = [[mtlBundle classNamed:@"MTLVertexBufferLayoutDescriptor"] new];
       streamDesc.stride = 32;
       streamDesc.stepRate = 1;
       streamDesc.stepFunction = MTLVertexStepFunctionPerVertex;

       //stagein layout
       g_VertexDesc = [MTLVertexDescriptor new];
       g_VertexDesc.attributes[0] = PosAttrDesc;
       g_VertexDesc.attributes[1] = UVAttrDesc;
       g_VertexDesc.layouts[4] = streamDesc;
}

void RenderAPI_Metal::CreateResources()
{
    id<MTLDevice> metalDevice = m_MetalGraphics->MetalDevice();
    NSError* error = nil;

    // Create shaders
    NSString* srcStr = [[NSString alloc] initWithBytes:kShaderSource length:sizeof(kShaderSource) encoding:NSASCIIStringEncoding];
    id<MTLLibrary> shaderLibrary = [metalDevice newLibraryWithSource:srcStr options:nil error:&error];
    if(error != nil)
    {
        NSString* desc        = [error localizedDescription];
        NSString* reason    = [error localizedFailureReason];
        ::fprintf(stderr, "%s\n%s\n\n", desc ? [desc UTF8String] : "<unknown>", reason ? [reason UTF8String] : "");
    }

    id<MTLFunction> vertexFunction = [shaderLibrary newFunctionWithName:@"vertexMain"];
    id<MTLFunction> fragmentFunction = [shaderLibrary newFunctionWithName:@"fragmentMain"];


    // Vertex / Constant buffers

#    if UNITY_OSX
    MTLResourceOptions bufferOptions = MTLResourceCPUCacheModeDefaultCache | MTLResourceStorageModeManaged;
#    else
    MTLResourceOptions bufferOptions = MTLResourceOptionCPUCacheModeDefault;
#    endif

    m_VertexBuffer = [metalDevice newBufferWithLength:1024 options:bufferOptions];
    m_VertexBuffer.label = @"PluginVB";
    m_ConstantBuffer = [metalDevice newBufferWithLength:16*sizeof(float) options:bufferOptions];
    m_ConstantBuffer.label = @"PluginCB";

    // Vertex layout
    MTLVertexDescriptor* vertexDesc = [MTLVertexDescriptorClass vertexDescriptor];
    vertexDesc.attributes[0].format            = MTLVertexFormatFloat3;
    vertexDesc.attributes[0].offset            = 0;
    vertexDesc.attributes[0].bufferIndex    = 1;
    vertexDesc.attributes[1].format            = MTLVertexFormatUChar4Normalized;
    vertexDesc.attributes[1].offset            = 3*sizeof(float);
    vertexDesc.attributes[1].bufferIndex    = 1;
    vertexDesc.layouts[1].stride            = kVertexSize;
    vertexDesc.layouts[1].stepFunction        = MTLVertexStepFunctionPerVertex;
    vertexDesc.layouts[1].stepRate            = 1;

    // Pipeline

    MTLRenderPipelineDescriptor* pipeDesc = [[MTLRenderPipelineDescriptorClass alloc] init];
    // Let's assume we're rendering into BGRA8Unorm...
    pipeDesc.colorAttachments[0].pixelFormat= MTLPixelFormatRGBA8Unorm;

    pipeDesc.depthAttachmentPixelFormat        = MTLPixelFormatInvalid;
    pipeDesc.stencilAttachmentPixelFormat    = MTLPixelFormatInvalid;

    pipeDesc.sampleCount = 1;
    pipeDesc.colorAttachments[0].blendingEnabled = NO;

    pipeDesc.vertexFunction        = vertexFunction;
    pipeDesc.fragmentFunction    = fragmentFunction;
    pipeDesc.vertexDescriptor    = vertexDesc;

    m_Pipeline = [metalDevice newRenderPipelineStateWithDescriptor:pipeDesc error:&error];
    if (error != nil)
    {
        ::fprintf(stderr, "Metal: Error creating pipeline state: %s\n%s\n", [[error localizedDescription] UTF8String], [[error localizedFailureReason] UTF8String]);
        error = nil;
    }

    // Depth/Stencil state
    MTLDepthStencilDescriptor* depthDesc = [[MTLDepthStencilDescriptorClass alloc] init];
    depthDesc.depthCompareFunction = GetUsesReverseZ() ? MTLCompareFunctionGreaterEqual : MTLCompareFunctionLessEqual;
    depthDesc.depthWriteEnabled = false;
    m_DepthStencil = [metalDevice newDepthStencilStateWithDescriptor:depthDesc];
    
    m_UsedTextureCount = 0;
}


RenderAPI_Metal::RenderAPI_Metal()
{
}


void RenderAPI_Metal::ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces)
{
    if (type == kUnityGfxDeviceEventInitialize)
    {
        m_MetalGraphics = interfaces->Get<IUnityGraphicsMetal>();
        MTLVertexDescriptorClass            = NSClassFromString(@"MTLVertexDescriptor");
        MTLRenderPipelineDescriptorClass    = NSClassFromString(@"MTLRenderPipelineDescriptor");
        MTLDepthStencilDescriptorClass      = NSClassFromString(@"MTLDepthStencilDescriptor");

        CreateResources();
        CreateRatemapResource();
        
        m_CommandQueue = [m_MetalGraphics->MetalDevice() newCommandQueue];
    }
    else if (type == kUnityGfxDeviceEventShutdown)
    {
        //@TODO: release resources
    }
}

void* RenderAPI_Metal::BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch)
{
    const int rowPitch = textureWidth * 4;
    // Just allocate a system memory buffer here for simplicity
    unsigned char* data = new unsigned char[rowPitch * textureHeight];
    *outRowPitch = rowPitch;
    return data;
}


void RenderAPI_Metal::EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr)
{
    id<MTLTexture> tex = (__bridge id<MTLTexture>)textureHandle;
    // Update texture data, and free the memory buffer
    
    [tex replaceRegion:MTLRegionMake3D(0,0,0, textureWidth,textureHeight,1) mipmapLevel:0 withBytes:dataPtr bytesPerRow:rowPitch];
    delete[](unsigned char*)dataPtr;
}

void RenderAPI_Metal::DoCopyTexture(void *sourceTexture, int sourceX, int sourceY, int sourceWidth, int sourceHeight, void *destinationTexture, int destinationX, int destinationY)
{
    id<MTLTexture> sourceTex = (__bridge id<MTLTexture>)sourceTexture;
    id<MTLTexture> destinationTex = (__bridge id<MTLTexture>)destinationTexture;
    
    //m_MetalGraphics->EndCurrentCommandEncoder();
    id<MTLCommandBuffer> commandBuffer = m_MetalGraphics->CurrentCommandBuffer();
    //id<MTLCommandBuffer> commandBuffer = [m_CommandQueue commandBuffer];
    
    id<MTLBlitCommandEncoder> blitCommand = [commandBuffer blitCommandEncoder];
    //[blitCommand optimizeContentsForGPUAccess:sourceTex];
    //[blitCommand optimizeContentsForGPUAccess:destinationTex];
    [blitCommand copyFromTexture:sourceTex sourceSlice:0 sourceLevel:0 sourceOrigin:MTLOriginMake(sourceX,sourceY,0) sourceSize:MTLSizeMake(sourceWidth, sourceHeight, 1) toTexture:destinationTex destinationSlice:0 destinationLevel:0 destinationOrigin:MTLOriginMake(destinationX,destinationY,0)];
    [blitCommand endEncoding];
    //[commandBuffer commit];
}

bool RenderAPI_Metal::CreateTexture(int width, int height, int pixelFormat, int textureIndex)
{
    
    MTLTextureDescriptor *textureDescriptor = [[MTLTextureDescriptor alloc] init];

    textureDescriptor.pixelFormat = MTLPixelFormatRGBA8Unorm;
    textureDescriptor.width = (unsigned int)width;
    textureDescriptor.height = (unsigned int)height;
    textureDescriptor.usage = MTLTextureUsageShaderRead | MTLTextureUsageRenderTarget;
    
    id<MTLTexture> t = [m_MetalGraphics->MetalDevice() newTextureWithDescriptor:textureDescriptor];
    
    if(textureIndex == m_UsedTextureCount)
    {
        m_Textures.push_back((__bridge_retained void*)t);
        m_UsedTextureCount++;
    }
    else
    {
        m_Textures[textureIndex] = (__bridge_retained void*)t;
    }
    
    return true;
}

void RenderAPI_Metal::DestroyTexture(int textureIndex)
{
    if(m_Textures[textureIndex] == nullptr) return;
    
    CFBridgingRelease(m_Textures[textureIndex]);
    m_Textures[textureIndex] = nullptr;
}

void* RenderAPI_Metal::GetTexturePointer(int textureIndex)
{
    return m_Textures[textureIndex];
}



void RenderAPI_Metal::DrawSimpleTriangles( id<MTLRenderCommandEncoder> cmd, const float worldMatrix[16], int triangleCount, const void* verticesFloat3Byte4)
{
    // Update vertex and constant buffers
    //@TODO: we don't do any synchronization here :)

    const int vbSize = triangleCount * 3 * kVertexSize;
    const int cbSize = 16 * sizeof(float);

    ::memcpy(m_VertexBuffer.contents, verticesFloat3Byte4, vbSize);
    ::memcpy(m_ConstantBuffer.contents, worldMatrix, cbSize);

#if UNITY_OSX
    [m_VertexBuffer didModifyRange:NSMakeRange(0, vbSize)];
    [m_ConstantBuffer didModifyRange:NSMakeRange(0, cbSize)];
#endif

    // Setup rendering state
    [cmd setRenderPipelineState:m_Pipeline];
    //[cmd setDepthStencilState:m_DepthStencil];
    [cmd setCullMode:MTLCullModeNone];

    // Bind buffers
    [cmd setVertexBuffer:m_VertexBuffer offset:0 atIndex:1];
    [cmd setVertexBuffer:m_ConstantBuffer offset:0 atIndex:0];

    // Draw
    [cmd drawPrimitives:MTLPrimitiveTypeTriangle vertexStart:0 vertexCount:triangleCount*3];
}

void RenderAPI_Metal::DrawColoredTriangle(id<MTLRenderCommandEncoder> cmd, float t)
{
    // Draw a colored triangle. Note that colors will come out differently
    // in D3D and OpenGL, for example, since they expect color bytes
    // in different ordering.
    struct MyVertex
    {
        float x, y, z;
        unsigned int color;
    };
    MyVertex verts[3] =
    {
        { -0.5f, -0.5f,  0, 0xFFff0000 },
        { 0.5f, -0.5f,  0, 0xFF00ff00 },
        { 0,     0.5f ,  0, 0xFF0000ff },
    };

    // Transformation matrix: rotate around Z axis based on time.
    float phi = t * 0.5f; // time set externally from Unity script
    float cosPhi = cosf(phi);
    float sinPhi = sinf(phi);
    float depth = 0.7f;
    float finalDepth = 1.0f - depth;
    float worldMatrix[16] = {
        cosPhi,-sinPhi,0,0,
        sinPhi,cosPhi,0,0,
        0,0,1,0,
        0,0,finalDepth,1,
    };

    DrawSimpleTriangles(cmd, worldMatrix, 1, verts);
}

void RenderAPI_Metal::DrawVRRBlit(void* sourceTex, void* targetTex)
{
    //m_MetalGraphics->EndCurrentCommandEncoder();
    
    id<MTLDevice> device = m_MetalGraphics->MetalDevice();
    NSBundle* mtlBundle = m_MetalGraphics->MetalBundle();

    //do postprocess pass
    if(nil == g_RatemapPassDesc)
    {
        g_RatemapPassDesc =  [[MTLRenderPassDescriptor alloc] init];
    }

    id<MTLTexture> outputTex = m_MetalGraphics->TextureFromRenderBuffer((UnityRenderBuffer)targetTex);
    id<MTLTexture> srcTex = m_MetalGraphics->TextureFromRenderBuffer((UnityRenderBuffer)sourceTex);

    g_RatemapPassDesc.colorAttachments[0].texture = outputTex;
    g_RatemapPassDesc.colorAttachments[0].loadAction = MTLLoadActionClear;
    g_RatemapPassDesc.colorAttachments[0].storeAction = MTLStoreActionStore;
    g_RatemapPassDesc.colorAttachments[0].clearColor = MTLClearColorMake(0, 0, 0, 0);

    g_RatemapPassDesc.depthAttachment.texture = nil;
    g_RatemapPassDesc.stencilAttachment.texture = nil;
    
    id<MTLCommandBuffer> buffer = [m_CommandQueue commandBuffer];
    id<MTLRenderCommandEncoder> commandEncoder = [buffer renderCommandEncoderWithDescriptor:g_RatemapPassDesc];

    //draw fullscreen triangle for ratemap upscale blit
    if(nil == g_RatemapPipelineDesc)
    {
        g_RatemapPipelineDesc = [MTLRenderPipelineDescriptor new];
    }
    
    g_RatemapPipelineDesc.colorAttachments[0].pixelFormat= MTLPixelFormatRGBA8Unorm;
    g_RatemapPipelineDesc.fragmentFunction = g_FProg;
    g_RatemapPipelineDesc.vertexFunction = g_VProg;
    g_RatemapPipelineDesc.vertexDescriptor = g_VertexDesc;
    g_RatemapPipelineDesc.sampleCount = 1;
    
    if(nil == g_VRRBlitPipelineState)
    {
        g_VRRBlitPipelineState = [device newRenderPipelineStateWithDescriptor:g_RatemapPipelineDesc error:nil];
    }
    

    [commandEncoder setRenderPipelineState:g_VRRBlitPipelineState];
    [commandEncoder setCullMode:MTLCullModeNone];
    [commandEncoder setVertexBuffer:g_VB offset:0 atIndex:4];
    [commandEncoder setFragmentBuffer:m_RatemapSizeBuffer offset:0 atIndex:0];
    [commandEncoder setFragmentTexture:srcTex atIndex:0];
    [commandEncoder drawPrimitives:MTLPrimitiveTypeTriangle vertexStart:0 vertexCount:6];
    [commandEncoder endEncoding];
    [buffer commit];
}

void RenderAPI_Metal::SetTextureColor(float red, float green, float blue, float alpha, void* targetTexture, float w, float h, float t)
{
    if(w == 0 || h == 0){
        return;
    }
    
    //m_MetalGraphics->EndCurrentCommandEncoder();
    
    MTLRenderPassDescriptor *rpdesc = [MTLRenderPassDescriptor renderPassDescriptor];
    rpdesc.colorAttachments[0].clearColor = MTLClearColorMake((double)red, (double)green, (double)blue, (double)alpha);
    rpdesc.colorAttachments[0].loadAction = MTLLoadActionClear;
    
    id<MTLTexture> colorRT = m_MetalGraphics->TextureFromRenderBuffer((UnityRenderBuffer)targetTexture);
    
    rpdesc.colorAttachments[0].texture = colorRT;
    
    id<MTLDevice> device = m_MetalGraphics->MetalDevice();
    
    if(nil == g_RateMap && w > 0 && h > 0)
    {
        MTLSize screenSize = MTLSizeMake(w, h, 0);
        MTLRasterizationRateMapDescriptor* descriptor = [[MTLRasterizationRateMapDescriptor alloc] init];
        descriptor.label = @"My rate map";
        descriptor.screenSize = screenSize;

        MTLSize zoneCounts = MTLSizeMake(3, 3, 1);
        MTLRasterizationRateLayerDescriptor* layerDescriptor = [[MTLRasterizationRateLayerDescriptor alloc] initWithSampleCount:zoneCounts];

        for (int row = 0; row < zoneCounts.height; row++)
        {
            layerDescriptor.verticalSampleStorage[row] = 1.0f;
        }
        for (int column = 0; column < zoneCounts.width; column++)
        {
            layerDescriptor.horizontalSampleStorage[column] = 1.0f;
        }
    
        layerDescriptor.verticalSampleStorage[0] = 0.001f;
        layerDescriptor.verticalSampleStorage[1] = 0.001f;
        //layerDescriptor.verticalSampleStorage[2] = 1.0f;
        layerDescriptor.horizontalSampleStorage[0] = 0.001f;
        //layerDescriptor.horizontalSampleStorage[1] = 0.001f;
        //layerDescriptor.horizontalSampleStorage[2] = 0.001f;
        
        /**layerDescriptor.verticalSampleStorage[0] = 0.15f;
        layerDescriptor.horizontalSampleStorage[1] = 0.15f;
        layerDescriptor.verticalSampleStorage[1] = 0.15f;
        layerDescriptor.horizontalSampleStorage[1] = 0.15f;
        layerDescriptor.verticalSampleStorage[2] = 0.15f;
        layerDescriptor.horizontalSampleStorage[1] = 0.15f;
        
        layerDescriptor.verticalSampleStorage[0] = 1.0f;
        layerDescriptor.horizontalSampleStorage[2] = 1.0f;
        layerDescriptor.verticalSampleStorage[1] = 1.0f;
        layerDescriptor.horizontalSampleStorage[2] = 1.0f;
        layerDescriptor.verticalSampleStorage[2] = 1.0f;
        layerDescriptor.horizontalSampleStorage[2] = 1.0f;**/

        [descriptor setLayer:layerDescriptor atIndex:0];
        g_RateMap = [device newRasterizationRateMapWithDescriptor: descriptor];
    }
    
    if(nil != g_RateMap)
    {
        rpdesc.rasterizationRateMap = g_RateMap;
        
        MTLSizeAndAlign rateMapParamSize = g_RateMap.parameterBufferSizeAndAlign;
        m_RatemapSizeBuffer = [device newBufferWithLength: rateMapParamSize.size
                                            options:MTLResourceStorageModeShared];
        [g_RateMap copyParameterDataToBuffer:m_RatemapSizeBuffer offset:0];
    }
    
   
    id<MTLCommandBuffer> buffer = [m_CommandQueue commandBuffer];
    id<MTLRenderCommandEncoder> commandEncoder = [buffer renderCommandEncoderWithDescriptor:rpdesc];
    DrawColoredTriangle(commandEncoder, t);
    [commandEncoder endEncoding];
    [buffer commit];
}

#endif // #if SUPPORT_METAL
