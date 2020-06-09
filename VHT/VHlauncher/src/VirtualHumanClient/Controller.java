package VirtualHumanClient;

import VHjava.VHCharacterReceiver;
import VHjava.VHCharacterSender;

public class Controller {
    private VHCharacterSender sender;
    private VHCharacterReceiver receiver;
    private String name;
    public Controller(VHCharacterSender sender, VHCharacterReceiver receiver) {
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
                VHCharacterSender.vhmsg.sendMessage("vrKillComponent all");
                break;
            }
            else if (s.contains("launcher requestPath")) {
                System.out.println("Someone is requesting working path");
                VHCharacterSender.vhmsg.sendMessage("launcher path " + System.getProperty("user.dir"));
            }
            else if (s.contains("launcher requestChar")) {
                System.out.println("Someone is requesting the character");
                VHCharacterSender.vhmsg.sendMessage("launcher char " + name);
            }
        }
    }
}
