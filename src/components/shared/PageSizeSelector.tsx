import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface PageSizeSelectorProps {
  currentPage: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  startIndex: number;
  endIndex: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  pageSizeOptions?: number[];
  showAllOption?: boolean;
}

export function PageSizeSelector({
  currentPage,
  totalPages,
  totalItems,
  pageSize,
  startIndex,
  endIndex,
  onPageChange,
  onPageSizeChange,
  hasPreviousPage,
  hasNextPage,
  pageSizeOptions = [20, 50, 100],
  showAllOption = true,
}: PageSizeSelectorProps) {
  const isShowAll = pageSize >= 10000;

  return (
    <div className="flex items-center justify-between gap-4 py-2 px-1 flex-wrap">
      {/* Left: Total items + page size */}
      <div className="flex items-center gap-3">
        <span className="text-sm text-muted-foreground whitespace-nowrap">
          {isShowAll 
            ? `${totalItems} item${totalItems !== 1 ? 's' : ''}`
            : `${startIndex + 1}–${endIndex} of ${totalItems}`
          }
        </span>
        <Select
          value={isShowAll ? 'all' : String(pageSize)}
          onValueChange={(val) => {
            if (val === 'all') {
              onPageSizeChange(99999);
            } else {
              onPageSizeChange(Number(val));
            }
          }}
        >
          <SelectTrigger className="h-8 w-[110px] text-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {pageSizeOptions.map((size) => (
              <SelectItem key={size} value={String(size)}>
                {size} / page
              </SelectItem>
            ))}
            {showAllOption && (
              <SelectItem value="all">Show All</SelectItem>
            )}
          </SelectContent>
        </Select>
      </div>

      {/* Right: Navigation arrows */}
      {!isShowAll && totalPages > 1 && (
        <div className="flex items-center gap-1">
          <Button
            variant="outline"
            size="icon"
            className="h-8 w-8"
            onClick={() => onPageChange(currentPage - 1)}
            disabled={!hasPreviousPage}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <span className="text-sm text-muted-foreground px-2 whitespace-nowrap">
            {currentPage} / {totalPages}
          </span>
          <Button
            variant="outline"
            size="icon"
            className="h-8 w-8"
            onClick={() => onPageChange(currentPage + 1)}
            disabled={!hasNextPage}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      )}
    </div>
  );
}
