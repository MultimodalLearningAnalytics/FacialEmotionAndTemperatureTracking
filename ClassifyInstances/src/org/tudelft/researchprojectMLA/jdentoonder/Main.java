package org.tudelft.researchprojectMLA.jdentoonder;

import java.io.*;
import java.nio.file.Files;
import java.util.Locale;
import java.util.Scanner;

import weka.classifiers.misc.InputMappedClassifier;
import weka.core.Instances;
import weka.core.SerializationHelper;
import weka.classifiers.trees.RandomForest;

import static java.lang.System.exit;

public class Main {
//    private static final String MODEL_NAME = "CombinedFeatures-EmotionsAndTemp-10s-Actual-Unbalanced";
//    private static final String MODEL_NAME = "DistractionFeatures-EmotionsAndTemp-20s-Relative";
    private static final String MODEL_NAME = "CombinedFeatures-EmotionsAndTemp-1s-Relative-Unbalanced";
    private static final String INPUT_FILE_NAME = "FullStoreFeatures-EmotionsAndTemp-1s-Relative";
    private static final String PATH_INPUT_FILE = "B:\\experiment_stores\\experiment3\\20210601-01\\participant2-01\\" + INPUT_FILE_NAME + ".arff";
    private static final String PATH_OUTPUT_FILE = "B:\\experiment_stores\\experiment3\\20210601-01\\participant2-01\\" + INPUT_FILE_NAME + "-" + MODEL_NAME + "-labeled.arff";
    private static final String PATH_MODEL_FILE = "B:\\weka_results\\models\\" + MODEL_NAME + ".model";

    public static void main(String[] args) throws Exception {
        System.out.println("Hello world!");

        if (new File(PATH_OUTPUT_FILE).exists()) {
            boolean validInput = false;
            while (!validInput) {
                System.out.println("Warning, output file already exists:");
                System.out.println(PATH_OUTPUT_FILE);
                System.out.println("Overwrite? [yn]");

                String yesOrNo = getConsoleInput();
                validInput = yesOrNo.toUpperCase().equals("Y") || yesOrNo.toUpperCase().equals("N");
                if (yesOrNo.toUpperCase().equals("N")) {
                    exit(0);
                }
            }
        }

        // Load unlabeled file
        Instances unlabeled = new Instances(new BufferedReader(new FileReader(PATH_INPUT_FILE)));
        unlabeled.setClassIndex(unlabeled.numAttributes() - 1);

        // Create copy of instances so we can label them
        Instances labeled = new Instances(unlabeled);
        labeled.setRelationName(unlabeled.relationName() + "-labeled");

        // Load the model
        InputMappedClassifier mappedClassifier = new InputMappedClassifier();
        mappedClassifier.setModelPath(PATH_MODEL_FILE);

        // label instances
        for (int i = 0; i < unlabeled.numInstances(); i++) {
            double clsLabel = mappedClassifier.classifyInstance(unlabeled.instance(i));
            labeled.instance(i).setClassValue(clsLabel);
        }

        // save labeled data
        BufferedWriter writer = new BufferedWriter(new FileWriter(PATH_OUTPUT_FILE));
        writer.write(labeled.toString());
        writer.newLine();
        writer.flush();
        writer.close();
    }

    public static String getConsoleInput() {
        Scanner scanner = new Scanner(System.in);
        return scanner.nextLine();
    }
}