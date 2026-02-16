import React from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { UserPlus, CheckSquare, Layers, FileText, Package, Building } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

interface QuickCreateModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function QuickCreateModal({ open, onOpenChange }: QuickCreateModalProps) {
  const navigate = useNavigate();
  const { t } = useTranslation();

  const openCreate = (path: string) => {
    onOpenChange(false);
    // navigate to the dedicated create page; those routes should exist or be handled
    navigate(path);
  };

  const actions = [
    { labelKey: 'quickCreate.newContact', icon: UserPlus, path: '/dashboard/contacts/add' },
    { labelKey: 'quickCreate.newTask', icon: CheckSquare, path: '/dashboard/tasks/add' },
    { labelKey: 'quickCreate.newProject', icon: Layers, path: '/dashboard/projects/add' },
    { labelKey: 'quickCreate.newOffer', icon: FileText, path: '/dashboard/offers/add' },
    { labelKey: 'quickCreate.newArticle', icon: Package, path: '/dashboard/inventory-services/add-article' },
    { labelKey: 'quickCreate.newInstallation', icon: Building, path: '/dashboard/field/installations/add' }
  ];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="w-full max-w-md p-0 overflow-hidden">
        <DialogHeader className="p-4 border-b border-border">
          <DialogTitle className="text-lg font-semibold">{t('quickCreate.title')}</DialogTitle>
        </DialogHeader>

        <div className="p-4 space-y-3">

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {actions.map((action, idx, arr) => {
              const Icon = action.icon as any;
              const isLastOdd = idx === arr.length - 1 && arr.length % 2 === 1;
              const containerClass = isLastOdd ? 'sm:col-span-2 sm:flex sm:justify-center' : '';

              return (
                <div key={action.labelKey} className={containerClass}>
                  <Button
                    onClick={() => openCreate(action.path)}
                    className={`justify-start text-white w-full ${isLastOdd ? 'sm:w-auto' : ''}`}
                  >
                    <Icon className="h-4 w-4 mr-2" />
                    {t(action.labelKey)}
                  </Button>
                </div>
              );
            })}
          </div>
        </div>

        <DialogFooter className="p-4 border-t border-border">
          <div className="w-full flex justify-end">
            <Button variant="ghost" onClick={() => onOpenChange(false)} className="mr-2">
              {t('quickCreate.cancel')}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export default QuickCreateModal;
