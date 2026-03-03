Imports System.Net.Http
Imports System.Text
Imports System.Text.Json

Namespace Modules
    Module ApiHelper
        Private apiBase As String = "http://localhost:8000"

        Private ReadOnly client As New HttpClient()

        Public Function GetDocuments() As List(Of Document)
            Dim response = client.GetStringAsync($"{apiBase}/documents").Result
            Dim jsonDoc = JsonDocument.Parse(response)
            Dim docs = New List(Of Document)
            For Each d In jsonDoc.RootElement.GetProperty("documents").EnumerateArray()
                docs.Add(New Document With {
                    .id = d.GetProperty("id").GetInt32(),
                    .document_path = d.GetProperty("document_path").GetString(),
                    .title_content = d.GetProperty("title_content").GetString(),
                    .created_at = d.GetProperty("created_at").GetString()
                })
            Next
            Return docs
        End Function

        ' Public Function GenerateQuestions(docId As Integer, sectionNames As List(Of String), numQuestions As Integer) As List(Of Question)
        '     Dim payload = New With {
        '         Key .document_id = docId,
        '         Key .section_names = sectionNames,
        '         Key .num_questions = numQuestions
        '     }
        '     Dim jsonPayload = JsonSerializer.Serialize(payload)
        '     Dim content As New StringContent(jsonPayload, Encoding.UTF8, "application/json")
        '     Dim response = client.PostAsync($"{apiBase}/generate-questions", content).Result
        '     Dim respStr = response.Content.ReadAsStringAsync().Result
        '     Dim jsonDoc = JsonDocument.Parse(respStr)
        '     Dim questions = New List(Of Question)
        '     For Each q In jsonDoc.RootElement.GetProperty("questions").EnumerateArray()
        '         Dim opts = q.GetProperty("options").EnumerateArray().Select(Function(x) x.GetString()).ToArray()
        '         questions.Add(New Question With {
        '             .section_name = q.GetProperty("section_name").GetString(),
        '             .question_text = q.GetProperty("question_text").GetString(),
        '             .options = opts,
        '             .correct_option = q.GetProperty("correct_option").GetInt32()
        '         })
        '     Next
        '     Return questions
        ' End Function
    End Module
End Namespace