package smartlab.test;

import smartlab.communication.ISLBytesSubscriber;

import java.util.Arrays;

public class TestByteSubscriber implements ISLBytesSubscriber {

    String name;
    public TestByteSubscriber(String name) {
        this.name = name;
    }

    @Override
    public void onReceive(String topic, byte[] content) {
        System.out.println("Received bytes message. Subscriber:" + this.name + "\tTopic: " + topic + "\tContent:" + Arrays.toString(content));
    }
}
