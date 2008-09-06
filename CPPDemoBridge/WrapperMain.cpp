#include <iostream>
#include "CPPTuioListenerWrapper.h"

using namespace std;
using namespace TUIO;
using namespace Grafiti;

ref class WrapperMain
{
private:
	TuioClient^ m_client;

public:
	WrapperMain(int port);
	~WrapperMain();
};

WrapperMain::WrapperMain(int port)
{
	m_client = gcnew TuioClient(port);
	CPPTuioListenerWrapper^ cppTuioListenerWrapper = gcnew CPPTuioListenerWrapper(new CPPTuioListener);

	Surface::Instance->SetGUIManager(cppTuioListenerWrapper);
	
	m_client->addTuioListener(Surface::Instance); // grafiti
	m_client->addTuioListener(cppTuioListenerWrapper); // wrapper's listener (will forward to unmanaged)
	m_client->connect();
}

WrapperMain::~WrapperMain()
{
	cout << "destruct" << endl;
	m_client->disconnect();
}

int main(int argc, char* argv[])
{
	gcnew WrapperMain(3333);
	return 0;
}