import { useState, useEffect } from 'react';
import { JarvisHeroScreen } from './components/JarvisHeroScreen';
import { Stage2Workspace } from './components/Stage2Workspace';
import { soundFX } from './utils/audio';

export default function App() {
  const [currentStage, setCurrentStage] = useState<1 | 2>(1);
  const [activeDirective, setActiveDirective] = useState<string>('');

  // Stage 1 -> Stage 2 only with a real directive from the user
  const handleStartBuild = (directive: string) => {
    const trimmed = directive.trim();
    if (!trimmed) return;
    setActiveDirective(trimmed);
    setCurrentStage(2);
  };

  const handleReturnToStage1 = () => {
    soundFX.playPowerDown();
    setCurrentStage(1);
  };

  const handleNewBuild = () => {
    soundFX.playIgnite();
    setActiveDirective('');
    setCurrentStage(1);
  };

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && e.altKey && currentStage === 2) {
        handleReturnToStage1();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [currentStage]);

  return (
    <div className="w-screen h-screen overflow-hidden bg-[#030508] text-[#F5F8FF] selection:bg-[#39B8FF]/30 font-sans">
      {currentStage === 1 ? (
        <JarvisHeroScreen onStartBuild={handleStartBuild} />
      ) : (
        <Stage2Workspace
          initialPrompt={activeDirective}
          onReturnToStage1={handleReturnToStage1}
          onNewBuild={handleNewBuild}
        />
      )}
    </div>
  );
}
