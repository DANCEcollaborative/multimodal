package smartlab.test;

import smartlab.communication.CommunicationManager;

public class TestCommunication {
    static public void main(String[] args) {
        CommunicationManager manager = new CommunicationManager();
        TestTextSubscriber textSubscriber = new TestTextSubscriber("TextSubscriber0");
        TestTextSubscriber textSubscriber2 = new TestTextSubscriber("TextSubscriber1");
        TestByteSubscriber byteSubscriber = new TestByteSubscriber("ByteSubscriber0");
        TestHybridSubscriber hybridSubscriber = new TestHybridSubscriber("HybridSubscriber0");
        manager.subscribe(textSubscriber, "test");
        manager.subscribe(textSubscriber2, "test");
        manager.subscribe(byteSubscriber, "testbytes");
        manager.subscribe(hybridSubscriber, "test");
        manager.subscribe(hybridSubscriber, "testbytes");
    }
}
