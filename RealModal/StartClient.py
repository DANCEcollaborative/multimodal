from utils.GlobalVaribles import GlobalVariables as GV

from communication.CommunicationManager import CommunicationManager as CM
from communication.Listener import ForwardVisualizer
from component.RemoteListener import FaceRecognitionListener, OpenPoseListener, PositionDisplayListener

import time

if __name__ == "__main__":
    # Initialize communication manager to receive massage from Psi.
    GV.manager = CM()

    # Add components and start the listener.
    visualizer = ForwardVisualizer(GV.manager, GV.client_addr_in, GV.client_addr_out, "PSI_Python_Image")
    if GV.UseFaceRecognition:
        RFR = FaceRecognitionListener("FaceRecognition")
        visualizer.add(RFR)
    if GV.UseOpenpose:
        ROP = OpenPoseListener("OpenPose")
        visualizer.add(ROP)
    if GV.UseFaceRecognition:
        RPD = PositionDisplayListener("Position")
        visualizer.add(RPD)
    visualizer.start()

    # Block the main process.
    while not GV.ended:
        pass

    exit(0)
