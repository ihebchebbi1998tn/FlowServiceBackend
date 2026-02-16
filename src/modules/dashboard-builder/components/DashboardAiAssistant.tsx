import { useState, useCallback, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'framer-motion';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';
import {
  Sparkles, Send, X, Loader2, CheckCircle2, AlertCircle,
  LayoutDashboard, ChevronRight, MessageSquarePlus, PanelRightClose,
} from 'lucide-react';
import { streamDashboardAI, AI_QUICK_PROMPTS, type StreamCallbacks } from '../services/dashboardAiService';
import type { DashboardWidget } from '../types';

interface Props {
  open: boolean;
  onClose: () => void;
  onApplyWidgets: (widgets: DashboardWidget[], name?: string) => void;
}

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  widgets?: DashboardWidget[] | null;
  isStreaming?: boolean;
  error?: boolean;
}

export function DashboardAiAssistant({ open, onClose, onApplyWidgets }: Props) {
  const { t } = useTranslation('dashboard');
  const ai = (key: string) => t(`dashboardBuilder.ai.${key}`);
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [isGenerating, setIsGenerating] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) setTimeout(() => inputRef.current?.focus(), 200);
  }, [open]);

  const scrollToBottom = () => {
    setTimeout(() => {
      scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
    }, 50);
  };

  const handleSend = useCallback(async (prompt?: string) => {
    const text = prompt || input.trim();
    if (!text || isGenerating) return;
    setInput('');

    const userMsg: Message = { id: `u-${Date.now()}`, role: 'user', content: text };
    const assistantMsg: Message = { id: `a-${Date.now()}`, role: 'assistant', content: '', isStreaming: true };

    setMessages(prev => [...prev, userMsg, assistantMsg]);
    setIsGenerating(true);
    scrollToBottom();

    const callbacks: StreamCallbacks = {
      onToken: (token) => {
        setMessages(prev => prev.map(m =>
          m.id === assistantMsg.id ? { ...m, content: m.content + token } : m
        ));
        scrollToBottom();
      },
      onComplete: (widgets) => {
        setMessages(prev => prev.map(m =>
          m.id === assistantMsg.id
            ? { ...m, isStreaming: false, widgets, content: widgets ? ai('generated') : m.content }
            : m
        ));
        setIsGenerating(false);
        scrollToBottom();
      },
      onError: (error) => {
        setMessages(prev => prev.map(m =>
          m.id === assistantMsg.id
            ? { ...m, isStreaming: false, error: true, content: error }
            : m
        ));
        setIsGenerating(false);
      },
    };

    await streamDashboardAI(text, callbacks);
  }, [input, isGenerating, ai]);

  const handleApply = (widgets: DashboardWidget[]) => {
    onApplyWidgets(widgets);
    onClose();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleNewConversation = () => {
    setMessages([]);
    setInput('');
  };

  return (
    <AnimatePresence>
      {open && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.15 }}
            className="fixed inset-0 z-40 bg-black/20 backdrop-blur-[2px]"
            onClick={onClose}
          />

          {/* Side panel */}
          <motion.div
            initial={{ x: '100%' }}
            animate={{ x: 0 }}
            exit={{ x: '100%' }}
            transition={{ type: 'spring', damping: 30, stiffness: 320 }}
            className="fixed top-0 right-0 z-50 h-full w-full max-w-[420px] bg-card border-l border-border shadow-2xl flex flex-col"
          >
            {/* Header */}
            <div className="flex items-center justify-between px-5 py-4 border-b border-border flex-shrink-0">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded-xl bg-primary/10 flex items-center justify-center">
                  <Sparkles className="h-4.5 w-4.5 text-primary" />
                </div>
                <div>
                  <h2 className="text-sm font-semibold text-foreground">{ai('panelTitle')}</h2>
                  <p className="text-[11px] text-muted-foreground">{ai('panelSubtitle')}</p>
                </div>
              </div>
              <div className="flex items-center gap-1">
                {messages.length > 0 && (
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={handleNewConversation}
                    className="text-muted-foreground hover:text-foreground"
                    title={ai('newConversation')}
                  >
                    <MessageSquarePlus className="h-4 w-4" />
                  </Button>
                )}
                <Button
                  variant="ghost"
                  size="icon-sm"
                  onClick={onClose}
                  className="text-muted-foreground hover:text-foreground"
                  title={ai('close')}
                >
                  <PanelRightClose className="h-4 w-4" />
                </Button>
              </div>
            </div>

            {/* Messages area */}
            <ScrollArea className="flex-1" ref={scrollRef}>
              <div className="p-4 space-y-4">
                {/* Empty state with suggestions */}
                {messages.length === 0 && (
                  <div className="space-y-4 pt-2">
                    <p className="text-xs text-muted-foreground leading-relaxed">
                      {ai('hint')}
                    </p>
                    <div className="space-y-1.5">
                      <p className="text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                        {ai('quickSuggestions')}
                      </p>
                      {AI_QUICK_PROMPTS.map((qp, i) => (
                        <button
                          key={i}
                          onClick={() => handleSend(qp.prompt)}
                          className="w-full flex items-center gap-3 px-3.5 py-3 rounded-lg border border-border/50 bg-background hover:bg-accent/50 hover:border-primary/25 transition-all text-left group"
                        >
                          <div className="w-8 h-8 rounded-lg bg-muted/60 group-hover:bg-primary/10 flex items-center justify-center shrink-0 transition-colors">
                            <LayoutDashboard className="h-3.5 w-3.5 text-muted-foreground group-hover:text-primary transition-colors" />
                          </div>
                          <span className="text-xs text-foreground font-medium flex-1">
                            {t(qp.labelKey)}
                          </span>
                          <ChevronRight className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-primary/50 transition-colors" />
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                {/* Chat messages */}
                {messages.map(msg => (
                  <div key={msg.id} className={cn('flex', msg.role === 'user' ? 'justify-end' : 'justify-start')}>
                    <div className={cn(
                      'max-w-[88%] rounded-xl px-3.5 py-2.5 text-[13px] leading-relaxed',
                      msg.role === 'user'
                        ? 'bg-primary text-primary-foreground rounded-br-md'
                        : 'bg-muted/50 text-foreground border border-border/30 rounded-bl-md'
                    )}>
                      {msg.isStreaming && !msg.content ? (
                        <div className="flex items-center gap-2.5 py-1">
                          <Loader2 className="h-3.5 w-3.5 animate-spin text-primary" />
                          <span className="text-muted-foreground text-xs">{ai('thinking')}</span>
                        </div>
                      ) : msg.error ? (
                        <div className="flex items-center gap-2.5">
                          <AlertCircle className="h-4 w-4 text-destructive shrink-0" />
                          <span className="text-destructive text-xs">{msg.content}</span>
                        </div>
                      ) : msg.widgets ? (
                        <div className="space-y-3">
                          <div className="flex items-center gap-2.5">
                            <CheckCircle2 className="h-4 w-4 text-primary shrink-0" />
                            <span className="font-medium text-xs">{msg.content}</span>
                          </div>
                          <div className="flex items-center gap-2 text-[11px] text-muted-foreground">
                            <LayoutDashboard className="h-3.5 w-3.5" />
                            {msg.widgets.length} {ai('widgetsGenerated')}
                          </div>
                          <Button
                            size="sm"
                            onClick={() => handleApply(msg.widgets!)}
                            className="w-full h-9 text-xs gap-2"
                          >
                            <Sparkles className="h-3.5 w-3.5" />
                            {ai('apply')}
                          </Button>
                        </div>
                      ) : (
                        <span className={msg.isStreaming ? 'opacity-70' : ''}>
                          {msg.content}
                          {msg.isStreaming && <span className="inline-block w-1 h-3.5 bg-primary/60 animate-pulse ml-0.5" />}
                        </span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </ScrollArea>

            {/* Input area */}
            <div className="p-4 border-t border-border flex-shrink-0">
              <div className="flex items-center gap-2">
                <Input
                  ref={inputRef}
                  value={input}
                  onChange={e => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder={ai('placeholder')}
                  disabled={isGenerating}
                  className="h-10 text-sm bg-background border-border/60 rounded-lg"
                />
                <Button
                  size="icon"
                  onClick={() => handleSend()}
                  disabled={!input.trim() || isGenerating}
                  className="h-10 w-10 rounded-lg shrink-0"
                >
                  {isGenerating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                </Button>
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}
