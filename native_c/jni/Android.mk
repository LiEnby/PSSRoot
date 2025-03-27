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

LOCAL_CFLAGS += -Os -ffreestanding -fno-exceptions -fno-ident -ffunction-sections -fdata-sections -fno-asynchronous-unwind-tables
LOCAL_LDFLAGS += -nostartfiles -nostdlib --gc-sections --strip-all -fno-exceptions

include $(BUILD_EXECUTABLE)

