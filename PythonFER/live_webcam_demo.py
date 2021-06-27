import cv2
import time
from fer import FER
from termcolor import colored

from PythonFER.TerminalColor import TerminalColor

detector = FER(mtcnn=True)


def do_capture(camera):
    while True:
        ret, frame = camera.read()
        if not ret:
            print(colored("Failed to grab image, retrying", TerminalColor.WARNING.value))
            camera.release()
            return False

        faces = detector.detect_emotions(frame)

        for i in range(len(faces)):
            box = faces[i]['box']
            x = box[0]
            y = box[1]
            face_width = box[2]
            face_height = box[3]

            cv2.rectangle(frame, (x, y), (x + face_width, y + face_height), (0, 0, 255), 2)

            offset = 32
            for emotion in faces[i]['emotions']:
                text = f"{emotion}: {faces[i]['emotions'][emotion]}"
                cv2.putText(frame, text, (x, y + face_height + offset), 0, 0.5, (0, 0, 255), 2)
                offset += 16

        cv2.imshow('frame', frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            return True


while True:
    cap = cv2.VideoCapture(0)
    if cap.isOpened():
        width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        fps = cap.get(cv2.CAP_PROP_FPS)

        print(colored(f"Camera connected: {width}x{height} @{fps} fps", TerminalColor.SUCCESS.value))
        res = do_capture(cap)
        if res:
            print(colored("Exiting...", TerminalColor.INFO.value))
            cap.release()
            cv2.destroyAllWindows()
            break
    else:
        print(colored("Camera failed to connect, retrying", TerminalColor.FAIL.value))
        cap.release()
        time.sleep(10)
        continue
