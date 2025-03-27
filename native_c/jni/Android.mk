LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_SRC_FILES := \
	dirtycow.c \
	dcow.c

LOCAL_MODULE := exploit
LOCAL_LDFLAGS   += -llog --strip-all --gc-sections
LOCAL_CFLAGS    += -O3

include $(BUILD_EXECUTABLE)

include $(CLEAR_VARS)
LOCAL_MODULE := payload
LOCAL_SRC_FILES := \
	run-as.c

LOCAL_CFLAGS += -Os -Wall -nostartfiles -nostdlib -fno-ident -fno-builtin \
		-fno-exceptions -fno-stack-protector
LOCAL_LDFLAGS += -nostdlib --gc-sections --strip-all

include $(BUILD_EXECUTABLE)

