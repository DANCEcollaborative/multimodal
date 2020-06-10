package smartlab.vhcommunication;

import smartlab.communication.ISLTextSubscriber;
import smartlab.vhmsgprocessor.*;

import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;

import javax.jms.MessageConsumer;

import org.apache.activemq.ActiveMQConnectionFactory;
/*
 * Subscriber the message from PSI.
 */
public class PSISubscriber implements ISLTextSubscriber{
	String name;
	VHSender sender = VHSender.getInstance();
	RendererController controller = RendererController.getInstance();
	VHMsgSpliter vhp = VHMsgSpliter.getInstance();
    NVBMsgProcessor nvbMsg = new NVBMsgProcessor();
    TextMsgProcessor textMsg = new TextMsgProcessor();
    BlockingQueue<Runnable> queue = new ArrayBlockingQueue<>(1000);    
    

    public PSISubscriber(String name) {
        this.name = name;
    }
    
    ThreadPoolExecutor executor = new ThreadPoolExecutor(
            Runtime.getRuntime().availableProcessors(), //corePoolSize
            20,
            120L,
            TimeUnit.SECONDS,
            queue
    );

    @Override
    /*
     * @param topic: String
     * the message topic in ActiveMQ.
     * @param content£º String
     * the text content from PSI
     */
    public void onReceive(String topic, String content) {

    	sender.setChar(controller.getCharacter());    	
		String type = vhp.typeGetter(content);
		String identity = vhp.identityGetter(content);
		if (type != null) {
			System.out.println("Received string message. Subscriber:" + this.name + "\tTopic: " + topic + "\tContent:" + content);
			executor.execute(new MessageTask(content,type));
		}
    }
	/*    public void listen() {
	    while (true) {
	        String s = receiver.pollVhmsg();
	        if (s.contains("vrProcEnd renderer")) {
	            System.out.println("now it's time to kill all components");
	            VHSender.vhmsg.sendMessage("vrKillComponent all");
	            receiver.stop();
	            break;
	        }
	    }
	}*/   

}
