package test;

import smartlab.communication.CommunicationManager;
import smartlab.test.TestByteSubscriber;
import smartlab.test.TestHybridSubscriber;
import smartlab.test.TestTextSubscriber;
import smartlab.vhcommunication.PSISubscriber;
import smartlab.vhcommunication.RendererController;
import smartlab.vhcommunication.VHReceiver;
import smartlab.vhcommunication.VHSender;

/*this is the main function to start the communication between VHT and PSI*/ 

public class PSI_VHTCommunicationStarter {
	static public void main(String[] args) { 
		RendererController controller = RendererController.getInstance();
	        CommunicationManager manager = new CommunicationManager();
	        PSISubscriber nvbmsg = new PSISubscriber("PSI_NVBG_Location");
	        PSISubscriber textmsg = new PSISubscriber("PSI_VHT_Text");
	        manager.subscribe(nvbmsg, "PSI_NVBG_Location");
	        manager.subscribe(textmsg, "PSI_VHT_Text");
	        //controller.listen();
	        //System.exit(0);
	    }
    }
