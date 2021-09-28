import numpy as np
import matplotlib.pyplot as plt 
import argparse

ap = argparse.ArgumentParser()
ap.add_argument("-f", "--motorfile", required=True, help="name of motor data file")
args = vars(ap.parse_args())

plt.rcParams['agg.path.chunksize'] = 20000

def ReadInsertionData():
    MotorFile = args["motorfile"]
    # PIDFile = args["pidfile"]
    prevRoll = 0
    prevRCM = 0
    prevtime = 0

    time = []
    vel = []
    RCM = []
    RCMVel = []
    EARoll = []
    EARollVel = []
    rcmVelandEARollVelTime = []

    with open(MotorFile) as f:
        lines = f.readlines()

        for i in range(len(lines)): 
            line = lines[i]
            t = float(line.split()[0])
            velVal = float(line.split()[1])
            rcm = float(line.split()[2])

            time.append(t)
            vel.append(velVal)
            RCM.append(rcm)



    fig, axs = plt.subplots(2,1)
    axs[0].plot(time,vel,'k')
    axs[1].plot(time,RCM,'k')

    axs[0].set_ylabel(r'$V_{EA}$ (mm/s)', fontsize = 12)
    axs[1].set_ylabel(r'$\delta_{EA}$ (deg)', fontsize = 12)
    axs[1].set_xlabel("Time (s)", fontsize = 12)

    print("Max Speed: ", max(vel))
    print("Max RCM: ", max(RCM))

    plt.show()


if __name__ == '__main__':
	ReadInsertionData()
