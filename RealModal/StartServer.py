from Socket.Server import *
from utils.FaceRecognitionUtil import FaceRecognitionUtil as FRU
from utils.OpenPoseUtil import OpenPoseUtil as OPU

if __name__ == "__main__":
    # Initialize modules:
    if GV.UseFaceRecognition:
        GV.fru = FRU()
    if GV.UseOpenpose:
        GV.opu = OPU()

    # Build and start server:
    addr_in = GV.server_addr_in
    server = Server(addr_in, ImageHandler)
    addr_out = GV.server_addr_out
    GV.send_server = SimpleTCPServer(addr_out)

    server.start()
