package vhCommunication;

import java.io.*;


public class RendererController {
    public RendererController() {
    }

    public void changeBackground(File img) {
        VHReceiver receiver = new VHReceiver();
        String vhtpath;
        VHSender.vhmsg.sendMessage("launcher requestPath");
        while (true) {
            String s = receiver.pollVhmsg();
            if (s.contains("launcher path")) {
                String[] temp = s.split(" ");
                System.out.println(temp[2]);
                vhtpath = temp[2];
                break;
            }
        }
        vhtpath = vhtpath + "\\bin\\vhtoolkitUnity\\vhtoolkitUnity_Data\\StreamingAssets\\Backgrounds\\";
        File newfile = new File(vhtpath + File.separator + img.getName());
        if (newfile.exists()) {
            newfile.delete();
            try {
                newfile.createNewFile();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        try {
            FileInputStream fin=new FileInputStream(img);//输入�?
            try {
                FileOutputStream fout=new FileOutputStream(newfile,true);//输出�?
                byte[] b =new byte[1024];
                try {
                    while((fin.read(b))!=-1) {//读取到末�? 返回-1 否则返回读取的字节个�?
                        fout.write(b);
                    }
                    fin.close();
                    fout.close();
                } catch (IOException e) {
                    e.printStackTrace();
                }

            } catch (FileNotFoundException e) {
                e.printStackTrace();
            }

        } catch (FileNotFoundException e) {
            e.printStackTrace();
        }
        VHSender.vhmsg.sendMessage("renderer background file " + img.getName());
        receiver.stop();
    }

    public void changeBackground(String path) {
        File img = new File(path);
        changeBackground(img);
    }

    public String getCharacter() {
        VHReceiver receiver = new VHReceiver();
        VHSender.vhmsg.sendMessage("launcher requestChar");
        while (true) {
            String s = receiver.pollVhmsg();
            if (s.contains("launcher char")) {
                String[] temp = s.split(" ");
                System.out.println(temp[2]);
                return temp[2];
            }
        }
    }
}
