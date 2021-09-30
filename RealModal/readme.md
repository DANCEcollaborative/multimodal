# Realmodal

### Introduction

This repository is designed for all the python development, namely **RealModal**, including framework, submodules, etc.

Realmodal is originally designed to process multimodal data streams in real-time with attachable processors. It receives
data stream from [PSI](https://github.com/DANCEcollaborative/PSI) or other modules and send back the processed 
information.

Realmodal contains two parts, **Realmodal Client** and **Realmodal Server**, which can be required to run on the same or 
two different machines.
* **Realmodal Client**: designed to communicate directly with other modules within the whole project via ActiveMQ. It 
can receive real-time multimodal streaming data, forward them to the server or process them locally, and send back the 
results back.
* **Realmodal Server**: designed for higher-load processing works including works with GPU usage. It receives data from 
Realmodal Client, process the multimodal data with complex processors including deep learning methods and send the 
result back to the client. 

### Quick Start
Following this quick start, you will be able to run the [demo](https://aclanthology.org/2020.sigdial-1.31.pdf) we
present on Sigdial 2020. In this demo, Realmodal is only required to communicate with PSI. So as long as you run PSI at
the same time, you can get a quick demonstration, but make sure you also get other modules running correctly for the
complete demo experience.
#### Requirements for both Client and Server
* python >= 3.7
* Required packages are listed in ```requirement.txt```. You can install all the requirements by running:
```shell script
pip install -r requirement.txt
```
* Sometimes, the ```stomp.py``` package might not be properly installed using the previous shell command. You might need 
to install it manually from its official website or specify a different version. 
 
#### Deployment of server
* Install the requirements listed above. 
* Install [OpenPose](https://github.com/CMU-Perceptual-Computing-Lab/openpose) if you would like to use the body pose
estimation functions.
* You may also need more packages if you'd like to use other kinds of processors, e.g., Tensorflow, PyTorch. However, 
the `requirement.txt` and OpenPose are sufficient for this quick start. 
* Change the configurations under `config/config.yaml` according to your running environment. If you don't know where to
start, leave them there. But make sure the address for the server is properly set (address-ip, address-port). To learn 
the meaning of the configuration items, refer to the [full document](doc/document.md#configuration-details)  
* Start the Realmodal Server on your server using the command:
```shell script
python3 StartServer.py
```
or specify your use of GPUs using the command:
```shell script
CUDA_VISIBLE_DEVICES=0,1 python3 StartServer.py
```

#### Running the client
* Install the requirements listed above. 
* Make sure the `config/config.yaml` is synchronized with that on the server.
* Run the client using the command 
```shell script
python3 StartClient.py
```
You will see `timeout` periodically if PSI is not running. This means the Realmodal Client is trying to fetch messages
from ActiveMQ. When PSI starts to run, a visualization of the room layout and camera will pop out. 

### Full document
If you'd like to learn about the configurations, modify the existing parts, add new parts, or learn how Realmodal is
working, please refer to the [full document](doc/document.md).    

### Common Questions
**Q:  I meet errors when installing and using OpenPose on the server, how should I proceed?**

A: Most errors we meet are due to the mismatched cuda setting, which includes: 
* The cmake process can't find CuDNN, i.e., the error is: 
```shell script
... ... 
-- cuDNN not found
-- Found gflags  (include: /usr/include, library: /usr/lib/x86_64-linux-gnu/libgflags.so)
-- Found glog    (include: /usr/include, library: /usr/lib/x86_64-linux-gnu/libglog.so)
-- Found Protobuf: /usr/lib/x86_64-linux-gnu/libprotobuf.so;-lpthread (found version "3.0.0") 
-- cuDNN not found.
... ... 
```
   Check whether you have your CUDA and CuDNN installed on your machine. If so, you can add the following environment
   variable before running `cmake` to specify the use of cuda:
```shell script
export 
```
* The version of GNU is not supported. This is because the version of your `gcc/g++/cc/c++` is not compatible with Cuda.
Please specify another version of cuda or choose a lower version of `gcc/g++/cc/c++` instead.  
* Realmodal can't find openpose, the error is 
```
Error: OpenPose library could not be found. Did you enable `BUILD_PYTHON` in CMake and have this Python script in the right folder?
```
or 
```
ImportError: cannot import name 'pyopenpose' from partially initialized module 'openpose' (most likely due to a circular import)
```
Make sure you build the python modules of OpenPose to use this project. To do so, you need to compile OpenPose 
twice and at the second time, you need to set `-DBUILD_PYTHON=ON` when doing `c+make`. See document 
[here](https://github.com/CMU-Perceptual-Computing-Lab/openpose/blob/1f1aa9c59fe59c90cca685b724f4f97f76137224/doc/03_python_api.md#installation)

Also, if you are using virtual environment such as conda when installing OpenPose, remember to specify the python binary
file and python library file you're using, see issue [here](https://github.com/CMU-Perceptual-Computing-Lab/openpose/issues/1623).



### Todos
* Codes:
    * Add default configuration files for all parts so one does not need to configure everything before using.
    * Add support for `argparse` so it can receive configurations from the command line.
    * Use a better logging framework for a better debugging experience.
* Comments and documents:  
    * Add more comments for core methods.
    * Add a demonstration on how to add classes to the document.
