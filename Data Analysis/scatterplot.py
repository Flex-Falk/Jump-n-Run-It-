import pandas as pd
import glob
import seaborn as sns
import matplotlib.pyplot as plt
import matplotlib as mpl
import os
import fnmatch
import mplcursors

def list_paths(folder='.', pattern='*', case_sensitive=False, subfolders=False):
    """Return a list of the file paths matching the pattern in the specified
    folder, optionally including files inside subfolders.
    """
    match = fnmatch.fnmatchcase if case_sensitive else fnmatch.fnmatch
    walked = os.walk(folder) if subfolders else [next(os.walk(folder))]
    return [os.path.join(root, f)
            for root, dirnames, filenames in walked
            for f in filenames if match(f, pattern)]

def save(df, index_collection, title):
    for index in index_collection:
        df.loc[index, "Action"] = "Crouch"

    output_csv_file_path = f'./relabeled/{title}.csv'
    df.to_csv(output_csv_file_path, index=False)

def plot_imu_data(df, df_root, title):
    df = df.copy()
    fig, axes = plt.subplots(2, 3, figsize=(15, 10), picker=True)
    fig.suptitle(title, fontsize=12)
    y_vars = df.columns.tolist()

    index_collection = []
    for ax, y_var in zip(axes.flatten(), y_vars):
        sc = sns.scatterplot(data=df, x=range(0, df.shape[0]), y=y_var, ax=ax, hue="action")
        ax.set_title(f'index vs {y_var}')
        cursor = mplcursors.cursor(sc)

        @cursor.connect("add")
        def on_click(sel):
            index = sel.index # something about the index does not work
            index_collection.append(index)
            print(f'Selected: Index {index}, Value {df.iloc[index].tolist()}')

    plt.tight_layout()
    #plt.title(label=title)
    plt.show()

    #save(df_root, index_collection, title)

def clear_action(df):
    df["Action"] = "Neutral"

def plot_each_seq():
    files = list_paths(folder=".", pattern='*.csv')
    for file in files:
        pdFrame = pd.read_csv(file, sep=",");
        groupedDf = pdFrame.groupby(pdFrame.Type)
        df_gyro = groupedDf.get_group("Gyro")
        #clear_action(df_gyro)
        df_accel = groupedDf.get_group("Accel")
        #clear_action(df_accel)
        plot_imu_data(df_gyro, pdFrame, file + " - gyro")
        plot_imu_data(df_accel, pdFrame, file + " - accel")

def plot_gyro_accel():
    files = list_paths(folder="./relabeled/", pattern='*.csv')
    for file in files:
        print(file)
        pdFrame = pd.read_csv(file, sep=",");
        groupedDf = pdFrame.groupby(pdFrame.Type)
        try:
            df_gyro = groupedDf.get_group("Gyro")
            plot_imu_data(df_gyro, pdFrame, file + " - gyro")
        except:
            df_accel = groupedDf.get_group("Accel")
            plot_imu_data(df_accel, pdFrame, file + " - accel")

#plot_each_seq()
#plot_gyro_accel()

def convert_pdrames(file):
    pdFrame = pd.read_csv(file, sep=",");
    mergePd = pd.DataFrame(columns=["x", "y", "z", "yaw", "pitch", "roll", "action"])
    print("Merged size expected to be:", pdFrame.shape[0]/2)
    for i in range(0, pdFrame.shape[0], 2):
        if(i > pdFrame.shape[0] or i+1 > pdFrame.shape[0]):
            break
        accel_index = -1
        gyro_index = -1
        action = "Neutral"
        try:
            if pdFrame.loc[i]["Type"] == "Accel":
                accel_index = i
            elif pdFrame.loc[i+1]["Type"] == "Accel":
                accel_index = i+1
            if pdFrame.loc[i]["Type"] == "Gyro":
                gyro_index = i
            elif pdFrame.loc[i+1]["Type"] == "Gyro":
                gyro_index = i+1
        except:
            pass
        if accel_index == -1 or gyro_index == -1:
            print("continue")
            continue
        if pdFrame.loc[i]["Action"] == pdFrame.loc[i+1]["Action"]:
            action = pdFrame.loc[i]["Action"]

        row = [
            pdFrame.loc[accel_index]["x"],
            pdFrame.loc[accel_index]["y"],
            pdFrame.loc[accel_index]["z"],
            pdFrame.loc[gyro_index]["x"],
            pdFrame.loc[gyro_index]["y"],
            pdFrame.loc[gyro_index]["z"],
            action
        ]
        #print(row)
        mergePd.loc[int(i/2)] = row

    print(mergePd)
    return mergePd

def convertFiles(folder):
    filesToConvert = list_paths(folder, "*.csv")
    print(filesToConvert)
    for file in filesToConvert:
        print(file)
        convert_pdrames(file).to_csv("converted_" + file.split(".\\").pop(), index=False)

#convertFiles(".")

def plot_converted(folder):
    filesToConvert = list_paths(folder, "*.csv")
    print(filesToConvert)
    for file in filesToConvert:
        print(file)
        plot_imu_data(pd.read_csv(file), None, file)



def set_all_actions_to_neutral(folder):
    filesToConvert = list_paths(folder, "*.csv")
    for file in filesToConvert:
        print(file)
        df = pd.read_csv(file)
        df["action"] = "Neutral"
        plot_imu_data(df, None, file)

#set_all_actions_to_neutral("./Datasets")


def set_labels_by_range():
    file = "./Datasets/Combinations.csv"
    ranges = "ranges"
    label = "label"
    rangesAndLabels = [
	{
		ranges: [(197, 212), (236, 242), (618, 623), (756, 769), (911, 916), (1162, 1174)],
		label: "Jump_Forward"
	},
	{
		ranges: [(213, 218), (243, 251), (624, 629), (770, 776), (917, 923), (1175, 1179)],
		label: "Jump_Backward"
	},
	{
		ranges: [(337, 342), (370, 374), (644, 651), (879, 886), (1009, 1015), (1304, 1309)],
		label: "Shoot_Forward"
	},
	{
		ranges: [(343, 351), (375, 386), (652, 661), (887, 894), (1016, 1026), (1310, 1321)],
		label: "Shoot_Backward"
	},
	{
		ranges: [(482, 490), (518, 526), (789, 796), (1034, 1041), (1132, 1140), (1272, 1279)],
		label: "Crouch_Forward"
	},
	{
		ranges: [(491, 500), (527, 537), (797, 809), (1042, 1054), (1141, 1149), (1280, 1290)],
		label: "Crouch_Backward"
	}
]

    df = pd.read_csv(file)
    #dfpow = pd.DataFrame()
    df["action"] = "Neutral"
    """
    for col in df.columns:
        if col != "action":
            dfpow[col]  = (df[col]/df[col].abs() * (df[col].abs() - df[col].median()) * df[col].pow(2))/df[col].sum()
        else:
            dfpow[col] = df[col]
    """
    for rangeAndLabel in rangesAndLabels:
        print(rangeAndLabel)
        for [start, inclusiveEnd] in rangeAndLabel[ranges]:
            for i in range(start, inclusiveEnd+1):
                df.loc[i, "action"] = rangeAndLabel[label]
    df.to_csv(file[0:-4] + "_labeled.csv", index=False)
    plot_imu_data(df, None, file)

#set_all_actions_to_neutral(".")
#set_labels_by_range()
#plot_converted(".")
set_labels_by_range()
#plot_imu_data(pd.read_csv("./Datasets/Combinations.csv"), None, "./Datasets/Combinations.csv")