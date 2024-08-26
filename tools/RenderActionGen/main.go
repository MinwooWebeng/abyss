package main

import (
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

func readFile(path string) string {
	inputFile, _ := os.Open(path)
	defer inputFile.Close()

	data, _ := io.ReadAll(inputFile)
	return string(data)
}

func removeExtra(data string) string {
	d2 := data[strings.Index(data, "message RenderAction"):]
	d3 := d2[strings.Index(d2, "\n"):]
	d4 := d3[:strings.Index(d3, "oneof")]

	re := regexp.MustCompile(`[^\S\r\n]+`)
	d5 := re.ReplaceAllString(d4, " ")

	lines := strings.Split(d5, "\n")
	var result []string
	for _, line := range lines {
		trimmedLine := strings.TrimSpace(line)
		if !strings.HasPrefix(trimmedLine, "//") && len(trimmedLine) != 0 {
			result = append(result, line)
		}
	}

	return strings.Join(result, "")
}

func replaceTypes(data string) string {
	return strings.ReplaceAll(
		strings.ReplaceAll(
			strings.ReplaceAll(
				strings.ReplaceAll(
					data, "int32", "int",
				), "message", "public void",
			), "{", "(",
		), "}", ")>><<",
	)
}

func splitEntry(data string) []string {
	return strings.Split(data, ">><<")
}

// Converts snake_case to PascalCase
func toPascalCase(snake string) string {
	words := strings.Split(snake, "_")
	var pascalCase string
	for _, word := range words {
		if word == "" {
			continue
		}
		// Convert the first letter to uppercase and the rest to lowercase
		pascalCase += strings.ToUpper(string(word[0])) + strings.ToLower(word[1:])
	}
	return pascalCase
}

func formatFunction(input string) string {
	input = strings.TrimSpace(input)
	// Regex to match function name and parameters
	re := regexp.MustCompile(`public void (\w+)\s*\(([^)]+)\)`)
	matches := re.FindStringSubmatch(input)

	if len(matches) < 3 {
		return "Invalid input"
	}

	functionName := matches[1]
	params := matches[2]

	// Split parameters and remove default values
	paramList := strings.Split(params, ";")
	var newParams []string
	var paramNames []string
	var originalParamNames []string // Store original parameter names for right side of assignment
	for _, p := range paramList {
		if trimmed := strings.TrimSpace(p); trimmed != "" {
			equalIndex := strings.Index(trimmed, "=")
			if equalIndex != -1 {
				trimmed = trimmed[:equalIndex]
			}
			newParams = append(newParams, strings.TrimSpace(trimmed))
			paramName := strings.TrimSpace(strings.Split(trimmed, " ")[1])
			paramNames = append(paramNames, toPascalCase(paramName))
			originalParamNames = append(originalParamNames, paramName) // Keep original format
		}
	}

	formattedParams := strings.Join(newParams, ",\n    ")

	// Build the formatted string with dynamic action construction
	actionBody := fmt.Sprintf("%s = new %s\n    {", functionName, functionName)
	for i, paramName := range paramNames {
		actionBody += fmt.Sprintf("\n        %s = %s,", paramName, originalParamNames[i])
	}
	actionBody = strings.TrimRight(actionBody, ",") + "\n    }"

	result := fmt.Sprintf("public void %s\n(\n    %s\n)\n=> Write(new RenderAction()\n{\n    %s\n});", functionName, formattedParams, actionBody)
	return result
}

func main() {
	data := readFile(os.Args[1])
	data = removeExtra(data)
	data = replaceTypes(data)
	result := make([]string, 0)
	split := splitEntry(data)
	for i, e := range split {
		if i == len(split)-1 {
			continue
		}
		result = append(result, formatFunction(e))
	}
	data = strings.Join(result, "\n")
	data = `using Google.Protobuf;
using static AbyssCLI.ABI.RenderAction.Types;

namespace AbyssCLI.ABI
{
    internal class RenderActionWriter(Stream stream)
    {

` + data + `

		public void Flush()
		{
			_out_stream.Flush();
		}

		private void Write(RenderAction msg)
		{
			var msg_len = msg.CalculateSize();
			_out_stream.Write(BitConverter.GetBytes(msg_len));
			msg.WriteTo(_out_stream);
		}

		private Stream _out_stream = stream;
	}
}`

	os.WriteFile(os.Args[2], []byte(data), 0644)
}
