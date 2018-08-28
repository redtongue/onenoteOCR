#include<stdio.h>
#include<stdlib.h>
#include<string.h>
#include<fstream>
#include<iostream>
using namespace std;
void change(){
	char buffer[256];
	char buffer_change[1024];
	ifstream in("test.txt");
	ifstream change("change.txt");
	ofstream out("result.txt");
	if(!in.is_open() || !change.is_open()){
		cout<<"error"; exit(0); 
	}
	int adex = 0;
	change.getline(buffer_change,1024);
	while(!in.eof()){
		in.getline(buffer,100);
		//buffer[0] = a[0];buffer[1] = a[1];
		if(buffer[2]!=' '){
			for(int i = strlen(buffer);i>1;i--){
				buffer[i] = buffer[i-1];
			}
		}
		buffer[0] = buffer_change[adex++];buffer[1] = buffer_change[adex++];
		out<<buffer<<endl;
	}
} 
void test(){
	char buffer[256];
	char buffer_change[1300];
	ifstream in("change.txt"); 
	ofstream out("result.txt");
	if(!in.is_open() || !out.is_open()){
		cout<<"error"; exit(0); 
	}
	in.getline(buffer_change,1300);
	int adex=0;
	for(int i = 0;i < 13;i++){
		for(int j = 0;j < 49;j++){
			int b = 62.5*i;
			int a = 28.5*j;
			//cout<<buffer_change[adex]<<buffer_change[adex+1];
			out<<buffer_change[adex]<<buffer_change[adex+1]<<" "<<a<<" "<<776-b-26<<" "<<a+26<<" "<<776-b<<" 0"<<endl;
			adex+=2;
			//£¨ºá×ø±ê£©+£¨776-×Ý×ø±ê-height£©+£¨ºá×ø±ê+width£©+£¨776-×Ý×ø±ê£©+0 
		}
	}
}
int main(){
	//change();
	test();
	return 0;
} 
