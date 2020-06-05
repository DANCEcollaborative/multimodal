package VirtualHumanClient;

import VHjava.VHReceiver;
import VHjava.VHSender;

public class test {
    public static void main(String[] args) {
        VHReceiver receiver = new VHReceiver();
        VHSender sender = new VHSender();
        CharacterLoader loader;
        String charname;
        if (args.length >= 1) {
            charname = args[0];
        }
        else {
            charname = "Brad";
        }
        loader = new CharacterLoader(sender, receiver, charname);
        loader.changeCharacter();
        Controller controller = new Controller(sender, receiver);
        controller.setName(charname);
        controller.listen();
        receiver.stop();
        System.exit(0);
    }
}