package test;

import smartlab.communication.CommunicationManager;
import smartlab.test.TestByteSubscriber;
import smartlab.test.TestHybridSubscriber;
import smartlab.test.TestTextSubscriber;

public class VHPsiNvbgReceivertest {

	static public void main(String[] args) {
        CommunicationManager manager = new CommunicationManager();
        TestTextSubscriber textSubscriber = new TestTextSubscriber("PSI_NVBG_Location");
        TestTextSubscriber textSubscriber2 = new TestTextSubscriber("TextSubscriber1");
        TestByteSubscriber byteSubscriber = new TestByteSubscriber("ByteSubscriber0");
        TestHybridSubscriber hybridSubscriber = new TestHybridSubscriber("HybridSubscriber0");
        manager.subscribe(textSubscriber, "PSI_NVBG_Location");
        manager.subscribe(textSubscriber2, "test");
        manager.subscribe(byteSubscriber, "testbytes");
        manager.subscribe(hybridSubscriber, "test");
        manager.subscribe(hybridSubscriber, "testbytes");
    }
}
