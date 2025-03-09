#include <unistd.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>

#include <fcntl.h>


//reduce binary size
char __aeabi_unwind_cpp_pr0[0];

void _start()
{
	setresgid(0, 0, 0);
	setresuid(0, 0, 0);
	system("/system/bin/sh -i");
	return;
}
