package smartlab.test;

import smartlab.communication.CommunicationManager;

public class TestCommunication {
    static public void main(String[] args) {
        CommunicationManager manager = new CommunicationManager();
        TestTextSubscriber textSubscriber = new TestTextSubscriber("PSI_Bazaar_Text");
        TestTextSubscriber textSubscriber2 = new TestTextSubscriber("PSI_VHT_Text");
        TestByteSubscriber byteSubscriber = new TestByteSubscriber("ByteSubscriber0");
        TestHybridSubscriber hybridSubscriber = new TestHybridSubscriber("HybridSubscriber0");
        manager.subscribe(textSubscriber, "PSI_Bazaar_Text");
        manager.subscribe(textSubscriber2, "PSI_VHT_Text");
        manager.subscribe(byteSubscriber, "testbytes");
        manager.subscribe(hybridSubscriber, "test");
        manager.subscribe(hybridSubscriber, "testbytes");
    }
}
