package test;

import smartlab.communication.CommunicationManager;
import smartlab.test.TestByteSubscriber;
import smartlab.test.TestHybridSubscriber;
import smartlab.test.TestTextSubscriber;
import vhCommunication.PSISubscriber;

/*this is the main function to start the communication between VHT and PSI*/ 

public class PSI_VHTCommunicationStarter {

	static public void main(String[] args) {
        CommunicationManager manager = new CommunicationManager();
        PSISubscriber nvbmsg = new PSISubscriber("PSI_NVBG_Location");
        PSISubscriber textmsg = new PSISubscriber("PSI_VHT_Text");
        manager.subscribe(nvbmsg, "PSI_NVBG_Location");
        manager.subscribe(textmsg, "PSI_VHT_Text");
    }
}
