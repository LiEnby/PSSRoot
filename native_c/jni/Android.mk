LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_SRC_FILES := \
	dirtycow.c \
	dcow.c

LOCAL_MODULE := exploit
LOCAL_LDFLAGS   += -llog
LOCAL_CFLAGS    += -DDEBUG

include $(BUILD_EXECUTABLE)

include $(CLEAR_VARS)
LOCAL_MODULE := payload
LOCAL_SRC_FILES := \
	run-as.c

LOCAL_CFLAGS += -Os -Wall -nostartfiles -nostdlib -fno-ident -fno-builtin \
		-fno-exceptions -fno-asynchronous-unwind-tables -fno-data-sections \
		-fomit-frame-pointer -fno-common -fno-fast-math -fno-inline-functions \
		-fshort-enums -fno-stack-protector -fno-strict-aliasing
LOCAL_LDFLAGS += -nostdlib --gc-sections --strip-all

include $(BUILD_EXECUTABLE)

