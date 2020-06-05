package smartlab.test;

import smartlab.communication.CommunicationManager;

public class PSISenderTest {
	static public void main(String[] args) {
		CommunicationManager manager = new CommunicationManager();
		manager.msgSender("Bazaar_PSI_Text", " I went ahead and updated to openJDK 11 and got jdk-11.0.7, which should be close enough. ");
		System.exit(0);
	}
}
