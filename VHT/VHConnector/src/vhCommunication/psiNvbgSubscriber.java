package vhCommunication;

import messageProcessor.vhMsgProcessor;
import smartlab.communication.ISLTextSubscriber;

public class psiNvbgSubscriber implements ISLTextSubscriber{
	String name;

    public psiNvbgSubscriber(String name) {
        this.name = name;
    }

    @Override
    public void onReceive(String topic, String content) {
    	VHSender sender = new VHSender();
    	RendererController controller = new RendererController();
    	sender.setChar(controller.getCharacter());    	
    	vhMsgProcessor vhp = new vhMsgProcessor();
		String type = vhp.typeGetter(content);
		String identity = vhp.identityGetter(content);
		String text = vhp.textGetter(content);
        System.out.println("Received string message. Subscriber:" + this.name + "\tTopic: " + topic + "\tContent:" + content);
        System.out.println("the information is ----"+ type+ identity+text);
        sender.sendMessage(text, type);
    }

	
}
