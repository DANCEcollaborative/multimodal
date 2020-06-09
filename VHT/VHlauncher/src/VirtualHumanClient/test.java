package VirtualHumanClient;

import VHjava.VHCharacterReceiver;
import VHjava.VHCharacterSender;

public class test {
    public static void main(String[] args) {
        VHCharacterReceiver receiver = new VHCharacterReceiver();
        VHCharacterSender sender = new VHCharacterSender();
        CharacterLoader loader;
        String charname;
        if (args.length >= 1) {
            charname = args[0];
        }
        else {
            charname = "Rachel";
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