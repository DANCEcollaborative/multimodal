# Python-Related Branch

This repository is designed for all the python development, namely **RealModal**, including framework, submodules, etc..


### usage
#### Requirements
* python >= 3.7
* Required packages are listed in ```requirement.txt```. You can install all the requirements by running:
```shell script
pip install -r requirement.txt
```
* Sometimes, the ```stomp.py``` package might not be properly installed using the previous shell command. You might need 
to install it manually from its official website. A newer version is OK for running Realmodal.
 
#### Deployment of server
* Install the requirements listed above. 
* Install [OpenPose](https://github.com/CMU-Perceptual-Computing-Lab/openpose) if you would like to use the body pose 
estimation functions.
* Run the server using command:
```shell script
python3 StartServer.py
```
or specify your use of gpus using command:
```shell script
CUDA_VISIBLE_DEVICES=0,1 python3 StartServer.py
```

#### Running the client
* Change the variables in ```GlobalVariables.py``` and ensure the ip address is matching your server.
* Run the client using command ```python3 StartClient.py```

### Progress

* Communication between Python and Psi (Done).
* Communication between Client and Server (Done).
* Add Face Recognition and Open Pose Module (Done).
* Add Positioning calculation Module (Done).
* Demo v2.0 (Done).
* Add comments and documents (In progress).
