package vhCommunication;

import smartlab.communication.ISLTextSubscriber;
import vhMsgProcessor.*;
/*
 * Subscriber the message from PSI.
 */

public class PSISubscriber implements ISLTextSubscriber{
	String name;
	VHSender sender = new VHSender();
	RendererController controller = new RendererController();
	VHMsgSpliter vhp = new VHMsgSpliter();
    NVBMsgProcessor nvbMsg = new NVBMsgProcessor();
    TextMsgProcessor textMsg = new TextMsgProcessor();
	

    public PSISubscriber(String name) {
        this.name = name;
    }


    @Override
    /*
     * @param topic : String
     * the message topic in ActiveMQ.
     * @param content £º String
     * the text content from PSI
     */
    
    public void onReceive(String topic, String content) {

    	sender.setChar(controller.getCharacter());    	
		String type = vhp.typeGetter(content);
		String identity = vhp.identityGetter(content);
		//String angle = nvbMsg.angleGetter(content);
		//String nvbmsg = nvbMsg.constructNVBMsg(angle, content);
        System.out.println("Received string message. Subscriber:" + this.name + "\tTopic: " + topic + "\tContent:" + content);
        sender.sendMessage(content, type);
    }

	
}
