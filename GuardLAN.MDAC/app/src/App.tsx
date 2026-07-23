import React, { useState } from "react";
import {
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";

export default function App() {
  const [deviceName, setDeviceName] = useState("");
  const [serverUrl, setServerUrl] = useState("http://10.0.2.2:5232");
  const [status, setStatus] = useState("Ready to register");

  const handleRegister = async () => {
    setStatus("Registering device...");

    try {
      const response = await fetch(`${serverUrl}/api/mdac/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ deviceName }),
      });

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      setStatus("Device registered successfully");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Registration failed");
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.title}>GuardLAN MDAC</Text>
        <Text style={styles.subtitle}>Consent-based mobile activity sync</Text>

        <View style={styles.card}>
          <Text style={styles.label}>Device name</Text>
          <TextInput
            style={styles.input}
            placeholder="e.g. Pixel 8"
            value={deviceName}
            onChangeText={setDeviceName}
          />

          <Text style={styles.label}>GuardLAN API URL</Text>
          <TextInput
            style={styles.input}
            placeholder="http://10.0.2.2:5232"
            value={serverUrl}
            onChangeText={setServerUrl}
          />

          <TouchableOpacity style={styles.button} onPress={handleRegister}>
            <Text style={styles.buttonText}>Register device</Text>
          </TouchableOpacity>

          <Text style={styles.status}>{status}</Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#0f172a",
  },
  content: {
    padding: 24,
    gap: 16,
  },
  title: {
    fontSize: 28,
    fontWeight: "700",
    color: "#f8fafc",
  },
  subtitle: {
    fontSize: 16,
    color: "#cbd5e1",
  },
  card: {
    backgroundColor: "#111827",
    borderRadius: 16,
    padding: 16,
    gap: 12,
  },
  label: {
    color: "#e2e8f0",
    fontWeight: "600",
  },
  input: {
    backgroundColor: "#1f2937",
    color: "#f8fafc",
    padding: 12,
    borderRadius: 10,
  },
  button: {
    backgroundColor: "#2563eb",
    paddingVertical: 12,
    borderRadius: 10,
    alignItems: "center",
  },
  buttonText: {
    color: "#fff",
    fontWeight: "700",
  },
  status: {
    color: "#93c5fd",
    marginTop: 4,
  },
});
