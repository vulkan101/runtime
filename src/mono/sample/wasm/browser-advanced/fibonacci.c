#include <emscripten/html5_webgl.h>
#include <stdio.h>
int Fibonacci(int n) {
	int fnow = 0, fnext = 1, tempf;
	while(--n>0){
		tempf = fnow + fnext;
		fnow = fnext;
		fnext = tempf;
	}
	return fnext;	
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
        const char* canvasIdStr ="MYCANVASID";
		printf("Canvas id: %s\n", canvasIdStr); fflush(stdout);
        context = emscripten_webgl_create_context(canvasIdStr, &attrs);
        printf("**OPENGL CONTEXT TEST Context: %d\n", context);fflush(stdout);
}
