import cv2
import numpy as np
#from matplotlib import pyplot as plt
PATTERN_SIZE = (6, 3)
img1 = cv2.imread('left_7.jpg',0)  #queryimage # left image
img2 = cv2.imread('right_7.jpg',0) #trainimage # right image

ret, cornersl = cv2.findChessboardCorners(img1, patternSize=PATTERN_SIZE)
print(cornersl)
ec = cv2.cvtColor(img1, cv2.COLOR_GRAY2BGR)
cv2.drawChessboardCorners(ec, PATTERN_SIZE, cornersl, ret)
cv2.imwrite("debug.png", ec)
cv2.imshow('debug', ec)
#cv2.waitKey()
ret, cornersr = cv2.findChessboardCorners(img2, patternSize=PATTERN_SIZE)
print(cornersr)


objp = np.zeros((PATTERN_SIZE[1]*PATTERN_SIZE[0], 3), dtype=np.float64)
objp[:, :2] = np.indices(PATTERN_SIZE).T.reshape(-1, 2)


F, mask = cv2.findFundamentalMat(cornersl, cornersr,cv2.FM_LMEDS)
print(F)

print('cc')
print(cornersl[0])
print(cornersl[0][0])
print(cornersl[0]+1)
cornersl.reshape(1,2);
print('cc1')
print(cornersl);
cc = np.array((cornersl[0][0], cornersl[0][1],1))
print(cc)
xf = np.dot(cc, F);
print(xf)
