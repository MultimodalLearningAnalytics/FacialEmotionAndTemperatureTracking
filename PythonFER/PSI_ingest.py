import io
import os
import time

import cv2
import msgpack
import numpy
import zmq
from PIL import Image
from fer import FER
from tensorflow.python.client import device_lib
import tensorflow as tf

tf.config.experimental.get_visible_devices()

print(device_lib.list_local_devices())
os.environ['CUDA_VISIBLE_DEVICES'] = '1'

zmqContext = zmq.Context()

sub_socket = zmqContext.socket(zmq.SUB)
sub_socket.connect("tcp://127.0.0.1:12345")
sub_socket.setsockopt_string(zmq.SUBSCRIBE, 'webcamFrames')

pub_socket = zmqContext.socket(zmq.PUB)
pub_socket.bind("tcp://127.0.0.1:12346")

# MTCNN Import error fixed by putting this in C:\Users\speed\miniconda3\envs\fer-windows\Lib\site-packages\mtcnn\network\factory.py
# from tensorflow.keras.layers import Input, Dense, Conv2D, MaxPooling2D, PReLU, Flatten, Softmax
# from tensorflow.keras.models import Model
detector = FER(mtcnn=True)

# Fix another incompatibility by removing compat.V1 in 2 lines in C:\Users\speed\miniconda3\envs\fer-windows\Lib\site-packages\fer\fer.py
# They then look like
# self.config = tf.ConfigProto(log_device_placement=False)
# self.__session = tf.Session(config=self.config, graph=self.__graph)

processed_frames = 0
start_time = None


while True:
    [topic, receivedPayload] = sub_socket.recv_multipart()
    if not start_time:
        start_time = time.time()

    message = msgpack.unpackb(receivedPayload, raw=True)
    frame = message[b'message']
    originatingTime = message[b'originatingTime']
    image = cv2.cvtColor(numpy.array(Image.open(io.BytesIO(bytearray(frame)))), cv2.COLOR_BGR2RGB)

    faces = detector.detect_emotions(image)

    sendPayload = {'message': faces, 'originatingTime': originatingTime}
    pub_socket.send_multipart(['faces'.encode(), msgpack.dumps(sendPayload)])

    if cv2.waitKey(1) % 256 == 27:  # press ESC to exit
        break

cv2.destroyAllWindows()
