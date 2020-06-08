import face_recognition
import cv2


class FaceRecognitionUtil():
    def __init__(self):
        self.known_faces = []
        self.scale = 1.0

    def recognize(self, img):
        """
        Recognize faces in a given image and try to match them with known faces.
        :param img: Input Image to recognize. (RGB Image, shape: height * width * channel)
        :return: face_rec, face_locations
        face_rec: matching result. A unique id is assigned to the same face.
        face_locations: locations of the faces. Each location is a tuple of rectangle: (top, right, bottom, left)
        """
        img = cv2.resize(img, None, fx=self.scale, fy=self.scale)
        
        face_locations = face_recognition.face_locations(img)
        face_encodings = face_recognition.face_encodings(img, face_locations)
        face_rec = []
        for i, face in enumerate(face_encodings):
            known_len = len(self.known_faces)
            if known_len == 0:
                self.known_faces.append(face)
                face_rec.append(0)
            else:
                results = face_recognition.compare_faces(self.known_faces, face)
                if not (True in results):
                    self.known_faces.append(face)
                    face_rec.append(len(self.known_faces) - 1)
                else:
                    for j, res in enumerate(results):
                        if res:
                            face_rec.append(j)
                            break
        ret_loc = []
        for i, face in enumerate(face_locations):
            ret_loc.append((int(face[0] / self.scale),
                            int(face[1] / self.scale),
                            int(face[2] / self.scale),
                            int(face[3] / self.scale)))
        return face_rec, ret_loc
