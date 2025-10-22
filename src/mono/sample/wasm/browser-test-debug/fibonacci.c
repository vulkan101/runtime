#include <emscripten/html5_webgl.h>
#include <stdio.h>

typedef struct 
{
     unsigned long long   index       : 19;
     unsigned long long   refCount    : 19;
     unsigned long long   type        :  5;
     unsigned long long   generation  :  9;   // [Sync-001]
     unsigned long long   state       :  2;
     unsigned long long   userData    : 10;
} MyHandle;

void PrintMyHandle(MyHandle* h)
{
    printf("MyHandle: index=%llu, refCount=%llu, type=%llu, generation=%llu, state=%llu, userData=%llu\n",
        (unsigned long long)(h)->index, (unsigned long long)(h)->refCount, (unsigned long long)(h)->type, (unsigned long long)(h)->generation, (unsigned long long)(h)->state, (unsigned long long)(h)->userData);
}

void TestMyHandle()
{
    MyHandle handle;
    handle.index = 1;
    handle.refCount = 2;
    handle.userData = 3;
    PrintMyHandle(&handle);
}

int Fibonacci(int n) {
	int fnow = 0, fnext = 1, tempf;
    printf("In Fibonacci.c\n"); fflush(stdout);
    EMSCRIPTEN_WEBGL_CONTEXT_HANDLE context = 0;
    EmscriptenWebGLContextAttributes attrs;
     attrs.depth = 1;
        attrs.stencil = 1;
        attrs.antialias = 1;
        attrs.premultipliedAlpha = 1;
        attrs.preserveDrawingBuffer = 0;
        attrs.powerPreference = EM_WEBGL_POWER_PREFERENCE_DEFAULT; //EM_WEBGL_POWER_PREFERENCE_HIGH_PERFORMANCE
        attrs.failIfMajorPerformanceCaveat = 0;
        attrs.majorVersion = 2;
        attrs.minorVersion = 0;
        attrs.enableExtensionsByDefault = 1;
        attrs.explicitSwapControl = 0;
        attrs.renderViaOffscreenBackBuffer = 0;
        attrs.proxyContextToMainThread = EMSCRIPTEN_WEBGL_CONTEXT_PROXY_DISALLOW;
        printf("Have attrs.majorVersion %d\n", attrs.majorVersion); fflush(stdout);        
        context = emscripten_webgl_create_context("#MyCanvasId", &attrs);
        printf("Got context: %d\n", (int)context);fflush(stdout);
	while(--n>0){
		tempf = fnow + fnext;
		fnow = fnext;
		fnext = tempf;
	}
	return fnext;	
}

bool TestBoolReturn(int input, int* outValue)
{
    *outValue = input * 2;
    return true;
}

void testGLStartup()
{
    EMSCRIPTEN_WEBGL_CONTEXT_HANDLE context = 0;
    EmscriptenWebGLContextAttributes attrs;
     attrs.depth = 1;
        attrs.stencil = 1;
        attrs.antialias = 1;
        attrs.premultipliedAlpha = 1;
        attrs.preserveDrawingBuffer = 0;
        attrs.powerPreference = EM_WEBGL_POWER_PREFERENCE_DEFAULT; //EM_WEBGL_POWER_PREFERENCE_HIGH_PERFORMANCE
        attrs.failIfMajorPerformanceCaveat = 0;
        attrs.majorVersion = 2;
        attrs.minorVersion = 0;
        attrs.enableExtensionsByDefault = 1;
        attrs.explicitSwapControl = 0;
        attrs.renderViaOffscreenBackBuffer = 0;
        attrs.proxyContextToMainThread = EMSCRIPTEN_WEBGL_CONTEXT_PROXY_DISALLOW;

        emscripten_console_log("Creating webGL context");
        const char* canvasIdStr = "myCanvasId";
		printf("Canvas id: %s\n", canvasIdStr); fflush(stdout);
        context = emscripten_webgl_create_context(canvasIdStr, &attrs);
        printf("**OPENGL CONTEXT TEST Context: %d\n", context);fflush(stdout);
}
