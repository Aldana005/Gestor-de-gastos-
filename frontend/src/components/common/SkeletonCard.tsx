import React from 'react';

interface SkeletonProps {
  className?: string;
  count?: number;
}

export const SkeletonBox: React.FC<{ className?: string }> = ({ className = 'h-6 w-full' }) => {
  return <div className={`skeleton-shimmer rounded-xl ${className}`} />;
};

export const SkeletonCard: React.FC<{ className?: string }> = ({ className = '' }) => {
  return (
    <div className={`glass-card p-5 space-y-3 ${className}`}>
      <div className="flex justify-between items-center">
        <SkeletonBox className="h-4 w-28" />
        <SkeletonBox className="h-8 w-8 rounded-full" />
      </div>
      <SkeletonBox className="h-8 w-40" />
      <SkeletonBox className="h-3 w-20" />
    </div>
  );
};

export const SkeletonList: React.FC<SkeletonProps> = ({ count = 3, className = '' }) => {
  return (
    <div className={`space-y-3 ${className}`}>
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="glass-card p-4 flex items-center justify-between">
          <div className="space-y-2 flex-1">
            <SkeletonBox className="h-4 w-36" />
            <SkeletonBox className="h-3 w-24" />
          </div>
          <SkeletonBox className="h-6 w-20" />
        </div>
      ))}
    </div>
  );
};
