from Socket.Server import *
from utils.FaceRecognitionUtil import FaceRecognitionUtil as FRU
from utils.OpenPoseUtil import OpenPoseUtil as OPU
from component.ServerProcessor import *

if __name__ == "__main__":
    # Initialize modules:
    if GV.UseFaceRecognition:
        GV.fru = FRU()
        add_processor(FaceRecognitionProcessor("FaceRecognition"))
    if GV.UseOpenpose:
        GV.opu = OPU()
        add_processor(OpenPoseProcessor("OpenPose"))
    if GV.UsePosition:
        add_processor(PositionProcessor(GV.PositionBackend, "Position"))

    # Build and start server:
    addr_in = GV.server_addr_in
    recv_server = Server(addr_in, ImageReceiveHandler)
    addr_out = GV.server_addr_out
    send_server = Server(addr_out, DataSendHandler)

    recv_server.start()
    send_server.start()
