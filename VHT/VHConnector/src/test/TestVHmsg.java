package test;
import messageProcessor.vhMsgProcessor;
import smartlab.communication.CommunicationManager;
import vhCommunication.RendererController;
import vhCommunication.VHReceiver;
import vhCommunication.VHSender;
import vhCommunication.psiNvbgSubscriber;

public class TestVHmsg {
	public static void main(String[] args) {
		VHSender sender = new VHSender();
		//VHReceiver reciver = new VHReceiver();
		// vhNvbgReceiver vhNvbgReceiver = new vhNvbgReceiver();
		// MessageProcessor processor = new MessageProcessor();
		RendererController controller = new RendererController();
		sender.setChar(controller.getCharacter());
		CommunicationManager manager = new CommunicationManager();
		psiNvbgSubscriber textmsg = new psiNvbgSubscriber("PSI_NVBG_Location");

		vhMsgProcessor vhp = new vhMsgProcessor();

		//String message = "hello,i am haogang";

		//String s = "Send location message to Bazaar:multimodal:false;%;identity:someone;%;text:hello,haogang";
		//String type = vhp.typeGetter(s);
				//String identity = vhp.identityGetter(s);
		// String[] coordinate = vhp.coordinateGetter(s);
		//String text1 = vhp.textGetter(s);
		//System.out.println(type + text1 + "11111111");
		// sender.sendMessage(text1, type);
		// for(String out: coordinate) { System.out.println(out); }
		/*
		 * double[] coordinate1 = vhp.angleCalculate(s); System.out.println(type);
		 * System.out.println(identity); for(double out: coordinate1) {
		 * System.out.println(out); }
		 */
		
		  manager.subscribe(textmsg, "PSI_NVBG_Location");

		//sender.sendMessage(message);
		//System.exit(0);
	}

}