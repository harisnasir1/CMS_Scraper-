import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import React, { useEffect } from 'react'

interface RichTextEditorProps {
  value: string
  onChange: (value: string) => void
  readOnly?: boolean
}

export function RichTextEditor({ value, onChange, readOnly = false }: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [StarterKit],
    content: value,
    onUpdate: ({ editor }) => {
      onChange(editor.getHTML())
    },
    editable: !readOnly,
    editorProps: {
      attributes: {
        class: 'prose prose-sm focus:outline-none max-w-none min-h-[150px] whitespace-pre-wrap'
      }
    }
  })

  useEffect(() => {
    if (editor && editor.getHTML() !== value) {
      editor.commands.setContent(value)
    }
  }, [value, editor])

  useEffect(() => {
    return () => {
      editor?.destroy()
    }
  }, [])

  if (!editor) {
    return (
      <div className="border rounded-md overflow-hidden">
        <div className="border-b px-3 py-2 flex items-center gap-2 bg-muted/5">
          <div className="h-6" />
        </div>
        <div className="min-h-[200px] flex items-center justify-center">
          <div className="text-sm text-muted-foreground">Loading editor...</div>
        </div>
      </div>
    )
  }

  const Toolbar = () => (
    <div className="flex items-center gap-2">
      <div className="flex items-center gap-1">
        <button
          onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
          className={`p-1.5 hover:bg-accent rounded-sm ${editor.isActive('heading', { level: 1 }) ? 'bg-accent' : ''}`}
        >
          H1
        </button>
        <button
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          className={`p-1.5 hover:bg-accent rounded-sm ${editor.isActive('heading', { level: 2 }) ? 'bg-accent' : ''}`}
        >
          H2
        </button>
        <button
          onClick={() => editor.chain().focus().setParagraph().run()}
          className={`p-1.5 hover:bg-accent rounded-sm ${editor.isActive('paragraph') ? 'bg-accent' : ''}`}
        >
          P
        </button>
      </div>

      <div className="h-4 w-px bg-border" />

      <div className="flex items-center gap-1">
        <button
          onClick={() => editor.chain().focus().toggleBold().run()}
          className={`p-1.5 hover:bg-accent rounded-sm font-bold ${editor.isActive('bold') ? 'bg-accent' : ''}`}
        >
          B
        </button>
        <button
          onClick={() => editor.chain().focus().toggleItalic().run()}
          className={`p-1.5 hover:bg-accent rounded-sm italic ${editor.isActive('italic') ? 'bg-accent' : ''}`}
        >
          I
        </button>
      </div>

      <div className="h-4 w-px bg-border" />

      <div className="flex items-center gap-1">
        <button
          onClick={() => editor.chain().focus().toggleBulletList().run()}
          className={`p-1.5 hover:bg-accent rounded-sm ${editor.isActive('bulletList') ? 'bg-accent' : ''}`}
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M3 5a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 5a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 5a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" clipRule="evenodd" />
          </svg>
        </button>
        <button
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
          className={`p-1.5 hover:bg-accent rounded-sm ${editor.isActive('orderedList') ? 'bg-accent' : ''}`}
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M4 5a1 1 0 011-1h10a1 1 0 110 2H5a1 1 0 01-1-1zm0 5a1 1 0 011-1h6a1 1 0 110 2H5a1 1 0 01-1-1zm0 5a1 1 0 011-1h3a1 1 0 110 2H5a1 1 0 01-1-1z" clipRule="evenodd" />
          </svg>
        </button>
      </div>

      <div className="h-4 w-px bg-border" />

      <div className="flex items-center gap-1">
        <button
          onClick={() => editor.chain().focus().undo().run()}
          className="p-1.5 hover:bg-accent rounded-sm"
          disabled={!editor.can().undo()}
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clipRule="evenodd" />
          </svg>
        </button>
        <button
          onClick={() => editor.chain().focus().redo().run()}
          className="p-1.5 hover:bg-accent rounded-sm"
          disabled={!editor.can().redo()}
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M7.707 3.293a1 1 0 010 1.414L5.414 7H11a7 7 0 017 7v2a1 1 0 11-2 0v-2a5 5 0 00-5-5H5.414l2.293 2.293a1 1 0 11-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clipRule="evenodd" />
          </svg>
        </button>
      </div>
    </div>
  )

  return (
    <div className="border rounded-md overflow-hidden">
      <div className="border-b px-3 py-2 flex items-center gap-2 bg-muted/5">
        {!readOnly && <Toolbar />}
      </div>
      <EditorContent
        editor={editor}
        className="prose prose-sm max-w-none p-3 min-h-[200px] focus:outline-none"
      />
    </div>
  )
}
