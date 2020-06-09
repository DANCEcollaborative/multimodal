package VirtualHumanClient;

import VHjava.VHCharacterReceiver;
import VHjava.VHCharacterSender;

import java.util.Arrays;

public class CharacterLoader {
    VHCharacterSender sender;
    VHCharacterReceiver receiver;
    String character;
    String[] vhmsgs;
    public CharacterLoader(VHCharacterSender sender, VHCharacterReceiver receiver)
    {
        this(sender, receiver, "Rachel");
    }

    public CharacterLoader(VHCharacterSender sender, VHCharacterReceiver receiver, String character) {
        this.sender = sender;
        this.receiver = receiver;
        this.character = character;
        if (this.character.equals("Rachel")) {
            this.vhmsgs = ("renderer destroyAllCharacters\n" +
                    "renderer create Rachel Rachel\n" +
                    "nvbg_set_option Rachel speaker_gesture true\n" +
                    "nvbg_set_option Rachel nvbg_POS_rules true\n" +
                    "renderer customizer v2 Rachel shirt settype solid\n" +
                    "renderer customizer v2 Rachel shirt setparam Color.r 0.8117647\n" +
                    "renderer customizer v2 Rachel shirt setparam Color.g 0.827451\n" +
                    "renderer customizer v2 Rachel shirt setparam Color.b 0.8313726\n" +
                    "renderer customizer v2 Rachel shirt settype solid\n" +
                    "renderer customizer v2 Rachel shirt setparam Color.r 0.8117647\n" +
                    "renderer customizer v2 Rachel shirt setparam Color.g 0.827451\n" +
                    "renderer customizer v2 Rachel shirt setparam Color.b 0.8313726\n" +
                    "sbm char * viseme au_1_left 0 0\n" +
                    "sbm char * viseme au_1_right 0 0\n" +
                    "sbm char * viseme au_2_left 0 0\n" +
                    "sbm char * viseme au_2_right 0 0\n" +
                    "sbm char * viseme au_4_left 0 0\n" +
                    "sbm char * viseme au_4_right 0 0\n" +
                    "sbm char * viseme au_5 0 0\n" +
                    "sbm char * viseme au_6 0 0\n" +
                    "sbm char * viseme au_7 0 0\n" +
                    "sbm char * viseme au_10 0 0\n" +
                    "sbm char * viseme au_12_left 0 0\n" +
                    "sbm char * viseme au_12_right 0 0\n" +
                    "sbm char * viseme au_26 0 0\n" +
                    "sbm char * viseme au_112 0 0\n" +
                    "sbm char * viseme au_130 0 0\n" +
                    "sbm char * viseme au_124 0 0\n" +
                    "sbm char * viseme au_129 0 0\n" +
                    "sbm char * viseme au_136 0 0\n" +
                    "sbm char * viseme au_103 0 0\n" +
                    "sbm char * viseme au_102 0 0\n" +
                    "sbm char * viseme au_101 0 0\n" +
                    "sbm char * viseme au_100 0 0\n" +
                    "sbm char * viseme au_126 0 0\n" +
                    "sbm char * viseme au_127 0 0\n" +
                    "sbm char * viseme au_131 0 0\n" +
                    "sbm char * viseme au_132 0 0\n" +
                    "sbm char * viseme au_133 0 0\n" +
                    "sbm char * viseme au_134 0 0\n" +
                    "vrAgentBML Rachel sbm_test_bml_5 start\n" +
                    "vrAgentBML Rachel sbm_test_bml_5 end complete\n" +
                    "vrAgentBML Rachel sbm_test_bml_6 start\n" +
                    "vrAgentBML Rachel sbm_test_bml_6 end complete\n" +
                    "vrAgentBML Rachel sbm_test_bml_4 start\n" +
                    "vrAgentBML Rachel sbm_test_bml_4 end complete persistent\n" +
                    "renderer background file ../../../../../CMU.jpg\n" +
                    "renderer background file ../../../../../CMU.jpg").split("\n");
        }
        else {
            this.vhmsgs = new String[0];
        }
    }

    public void changeCharacter() {
        while (true) {
            String s = receiver.pollVhmsg();
            if (s.contains("vrAgentBML Brad sbm_test_bml_1 end complete persistent")) {
                break;
            }
        }
        System.out.println("now it's time to change character");
        for (String s: vhmsgs) {
            System.out.println("Sending vhmsg: " + s);
            VHCharacterSender.vhmsg.sendMessage(s);
        }
    }
}
