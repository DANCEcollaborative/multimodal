package VirtualHumanClient;

import VHjava.VHReceiver;
import VHjava.VHSender;

public class Controller {
    private VHSender sender;
    private VHReceiver receiver;
    private String name;
    public Controller(VHSender sender, VHReceiver receiver) {
        this.sender = sender;
        this.receiver = receiver;
    }

    public void setName(String name) {
        this.name = name;
    }

    public void listen() {
        while (true) {
            String s = receiver.pollVhmsg();
            if (s.contains("vrProcEnd renderer")) {
                System.out.println("now it's time to kill all components");
                VHSender.vhmsg.sendMessage("vrKillComponent all");
                break;
            }
            else if (s.contains("launcher requestPath")) {
                System.out.println("Someone is requesting working path");
                VHSender.vhmsg.sendMessage("launcher path " + System.getProperty("user.dir"));
            }
            else if (s.contains("launcher requestChar")) {
                System.out.println("Someone is requesting the character");
                VHSender.vhmsg.sendMessage("launcher char " + name);
            }
        }
    }
}
