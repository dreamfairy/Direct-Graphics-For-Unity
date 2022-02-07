REM UNITY_ROOT should be set to folder with Unity repository
"%UNITY_ROOT%/build/EmbeddedLinux/llvm/bin/clang++" --sysroot="%UNITY_ROOT%/build/EmbeddedLinux/sdk-linux-x64/x86_64-embedded-linux-gnu/sysroot" -DUNITY_EMBEDDED_LINUX_GL=1 -O2 -fPIC -shared -rdynamic -o libRenderingPlugin.so -fuse-ld=lld.exe -Wl,-soname,RenderingPlugin -Wl,-lGL --gcc-toolchain="%UNITY_ROOT%/build/EmbeddedLinux/sdk-linux-x64" -target x86_64-embedded-linux-gnu ../../source/RenderingPlugin.cpp ../../source/RenderAPI_OpenGLCoreES.cpp ../../source/RenderAPI.cpp
