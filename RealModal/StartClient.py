from utils.GlobalVariables import GlobalVariables as GV

from communication.CommunicationManager import CommunicationManager as CM
from component.ForwardListener import ForwardVisualizer
from component.LocationListener import LocationQuerier
from component.RemoteListener import FaceRecognitionListener, OpenPoseListener, PositionDisplayListener
from component.DialogListener import DialogListener

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
    if GV.UsePosition:
        RPD = PositionDisplayListener("Position")
        visualizer.add(RPD)

    if GV.UseDepthCamera:
        LQ = LocationQuerier(GV.manager, "PSI_Python_AnswerKinect", "Python_PSI_QueryKinect")
        GV.LocationQuerier = LQ

    visualizer.start()

    # DL = DialogListener(GV.manager, "PSI_Bazaar_Text", "Python_PSI_Text", **GV.DialogAgentInfo)

    # Block the main process.
    while not GV.ended:
        time.sleep(2)
        pass

    exit(0)
