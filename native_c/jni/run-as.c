void _start()
{
	setresgid(0, 0, 0);
	setresuid(0, 0, 0);
	execve("/system/bin/sh", 0, 0);
}
