#ifndef MH_LOG_HEADER_
#define MH_LOG_HEADER_


#include <stdio.h>

#define MH_LOG(msg, ...) { \
  printf("MH_NATIVE_LOG: %s : %s | %d :: ", __func__, __FILE__, __LINE__); \
  printf((msg), ##__VA_ARGS__); \
  printf("\n"); \
  fflush(stdout); \
}

#endif
