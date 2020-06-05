package smartlab.test;

import smartlab.communication.ISLTextSubscriber;

import java.util.Arrays;

public class TestTextSubscriber implements ISLTextSubscriber {
    String name;

    public TestTextSubscriber(String name) {
        this.name = name;
    }

    @Override
    public void onReceive(String topic, String content) {
        System.out.println("Received string message. Subscriber:" + this.name + "\tTopic: " + topic + "\tContent:" + content);
    }
}
