package smartlab.test;

import smartlab.communication.ISLBytesSubscriber;
import smartlab.communication.ISLMessageSubscriber;
import smartlab.communication.ISLTextSubscriber;

import javax.jms.BytesMessage;
import javax.jms.Message;
import javax.jms.TextMessage;
import java.util.Arrays;

public class TestHybridSubscriber implements ISLMessageSubscriber {
    String name;

    public TestHybridSubscriber(String name) {
        this.name = name;
    }

    @Override
    public void onReceive(String topic, Message content) {
        try {
            if (content instanceof TextMessage) {
                System.out.println("Received string message. Subscriber:" + this.name + "\tTopic: " + topic + "\tContent:" + ((TextMessage) content).getText());
            }
            else if (content instanceof BytesMessage) {
                BytesMessage bm = (BytesMessage)content;
                byte[] temp = new byte[(int)bm.getBodyLength()];
                bm.readBytes(temp);
                System.out.println("Received bytes message. Subscriber:" + this.name + "\tTopic: " + topic + "\tContent:" + Arrays.toString(temp));
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
