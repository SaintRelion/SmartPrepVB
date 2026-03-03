Namespace Modules
    Public Class Document
        Public Property id As Integer
        Public Property document_path As String
        Public Property title_content As String
        Public Property created_at As String

        Public Overrides Function ToString() As String
            Return title_content
        End Function
    End Class

    Public Class ExamItem
        Public Property Id As Integer
        Public Property DisplayText As String
        Public Overrides Function ToString() As String
            Return DisplayText
        End Function
    End Class

    Public Class Section
        Public Property id As Integer
        Public Property section_name As String
    End Class

    Public Class ExamQuestion
        Public Property question_text As String
        Public Property choices As List(Of String)
        Public Property correct_answer As String
    End Class
End Namespace