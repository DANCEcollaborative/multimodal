package test;
import smartlab.communication.CommunicationManager;
import smartlab.vhcommunication.PSISubscriber;
import smartlab.vhcommunication.RendererController;
import smartlab.vhcommunication.VHReceiver;
import smartlab.vhcommunication.VHSender;
import smartlab.vhmsgprocessor.NVBMsgProcessor;
import smartlab.vhmsgprocessor.TextMsgProcessor;
import smartlab.vhmsgprocessor.VHMsgSpliter;

public class TestVHmsg {
	public static void main(String[] args) {
		VHSender sender = new VHSender();
		//VHReceiver reciver = new VHReceiver();
		// vhNvbgReceiver vhNvbgReceiver = new vhNvbgReceiver();
		RendererController controller = RendererController.getInstance();
		sender.setChar(controller.getCharacter());
		//CommunicationManager manager = new CommunicationManager();
		//psiNvbSubscriber textmsg = new psiNvbSubscriber("PSI_NVBG_Location");
		VHMsgSpliter vhp = VHMsgSpliter.getInstance();
	    NVBMsgProcessor nvbMsg = new NVBMsgProcessor();
	    TextMsgProcessor textMsg = new TextMsgProcessor();
		String s = "send message to : multimodal:false;%;identity:someone;%;text:ddddd";
		String type = vhp.typeGetter(s);
		String identity = vhp.identityGetter(s);
		String[] coordinate = vhp.coordinateGetter(s);
		//String text1 = vhp.textGetter(s);
		String location = nvbMsg.angleGetter(s);
		System.out.println(type + identity + location+ "11111111");
		sender.sendMessage(s, type);
		for(String out: coordinate) { System.out.println(out); }
		/*
		 * double[] coordinate1 = vhp.angleCalculate(s); System.out.println(type);
		 * System.out.println(identity); for(double out: coordinate1) {
		 * System.out.println(out); }
		 */
		
		 // manager.subscribe(textmsg, "PSI_NVBG_Location");

		//sender.sendMessage(message);
		System.exit(0);
	}

}