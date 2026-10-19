/* Minimal stand-in for Opus' os_support.h, which the rnnoise 0.2 release
   tarball references (vec.h, generic non-AVX path) but does not ship.
   Written for RoxyPad; only what the rnnoise sources use. */
#ifndef OS_SUPPORT_H
#define OS_SUPPORT_H

#include <stdlib.h>
#include <string.h>

static __inline void *opus_alloc(size_t size) { return malloc(size); }
static __inline void opus_free(void *ptr) { free(ptr); }

#define OPUS_COPY(dst, src, n) (memcpy((dst), (src), (n)*sizeof(*(dst)) + 0*((dst)-(src))))
#define OPUS_MOVE(dst, src, n) (memmove((dst), (src), (n)*sizeof(*(dst)) + 0*((dst)-(src))))
#define OPUS_CLEAR(dst, n) (memset((dst), 0, (n)*sizeof(*(dst))))

#endif
